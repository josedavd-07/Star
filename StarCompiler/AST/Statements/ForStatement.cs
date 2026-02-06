namespace StarCompiler.AST.Statements;

/// <summary>
/// Bucle For/Orbit: Orbit (init; condition; step) { ... }.
/// For loop: Orbit (init; condition; step) { ... }.
/// </summary>
public class ForStatement : Statement
{
    public Statement? Init { get; set; }
    public Expression Condition { get; set; }
    public Statement? Step { get; set; }
    public List<Statement> Body { get; set; }

    public ForStatement(
        Statement? init,
        Expression condition,
        Statement? step,
        List<Statement> body)
    {
        Init = init;
        Condition = condition;
        Step = step;
        Body = body;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        if (Init != null)
        {
            Init.Execute(interpreter);
        }

        while ((bool)Condition.Evaluate(interpreter))
        {
            foreach (var stmt in Body)
            {
                stmt.Execute(interpreter);
            }

            if (Step != null)
            {
                Step.Execute(interpreter);
            }
        }
    }
}
