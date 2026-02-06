namespace StarCompiler.AST.Statements;

/// <summary>
/// Declaración If/When: When (condition) { ... } ElseWhen { ... } Otherwise { ... }.
/// If statement: When (condition) { ... } ElseWhen { ... } Otherwise { ... }.
/// </summary>
public class IfStatement : Statement
{
    public Expression Condition { get; set; }
    public List<Statement> ThenBlock { get; set; }
    public List<(Expression Condition, List<Statement> Block)> ElseIfBlocks { get; set; }
    public List<Statement>? ElseBlock { get; set; }

    public IfStatement(
        Expression condition,
        List<Statement> thenBlock,
        List<(Expression, List<Statement>)> elseIfBlocks,
        List<Statement>? elseBlock = null)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseIfBlocks = elseIfBlocks;
        ElseBlock = elseBlock;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        if ((bool)Condition.Evaluate(interpreter))
        {
            foreach (var stmt in ThenBlock)
            {
                stmt.Execute(interpreter);
            }
            return;
        }

        foreach (var (cond, block) in ElseIfBlocks)
        {
            if ((bool)cond.Evaluate(interpreter))
            {
                foreach (var stmt in block)
                {
                    stmt.Execute(interpreter);
                }
                return;
            }
        }

        if (ElseBlock != null)
        {
            foreach (var stmt in ElseBlock)
            {
                stmt.Execute(interpreter);
            }
        }
    }
}
