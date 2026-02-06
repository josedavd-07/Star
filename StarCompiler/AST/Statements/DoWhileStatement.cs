namespace StarCompiler.AST.Statements;

/// <summary>
/// Bucle Do-While/SpinWhile: SpinWhile { ... } (condition).
/// Do-While loop: SpinWhile { ... } (condition).
/// </summary>
public class DoWhileStatement : Statement
{
    public List<Statement> Body { get; set; }
    public Expression Condition { get; set; }

    public DoWhileStatement(List<Statement> body, Expression condition)
    {
        Body = body;
        Condition = condition;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        do
        {
            foreach (var stmt in Body)
            {
                stmt.Execute(interpreter);
            }
        } while ((bool)Condition.Evaluate(interpreter));
    }
}
