namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión literal: números, strings, booleanos.
/// Literal expression: numbers, strings, booleans.
/// </summary>
public class LiteralExpression : Expression
{
    public object Value { get; set; }

    public LiteralExpression(object value)
    {
        Value = value;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        return Value;
    }
}
