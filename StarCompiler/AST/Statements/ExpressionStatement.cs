using StarCompiler.AST.Expressions;

namespace StarCompiler.AST.Statements;

/// <summary>
/// Una declaración que consiste en una sola expresión (como una llamada a función).
/// A statement consisting of a single expression (like a function call).
/// </summary>
public class ExpressionStatement : Statement
{
    public Expression Expression { get; set; }

    public ExpressionStatement(Expression expression)
    {
        Expression = expression;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        Expression.Evaluate(interpreter);
    }
}
