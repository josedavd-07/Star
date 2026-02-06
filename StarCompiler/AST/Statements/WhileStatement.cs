namespace StarCompiler.AST.Statements;

/// <summary>
/// Bucle While: While (condition) { ... }.
/// While loop: While (condition) { ... }.
/// </summary>
public class WhileStatement : Statement
{
    public Expression Condition { get; set; }
    public List<Statement> Body { get; set; }

    public WhileStatement(Expression condition, List<Statement> body)
    {
        Condition = condition;
        Body = body;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        while ((bool)Condition.Evaluate(interpreter))
        {
            foreach (var stmt in Body)
            {
                stmt.Execute(interpreter);
            }
        }
    }
}
