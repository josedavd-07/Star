using StarCompiler.Lexer;

namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión binaria: a + b, x > 5, etc.
/// Binary expression: a + b, x > 5, etc.
/// </summary>
public class BinaryExpression : Expression
{
    public Expression Left { get; set; }
    public TokenType Operator { get; set; }
    public Expression Right { get; set; }

    public BinaryExpression(Expression left, TokenType op, Expression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        var leftVal = Left.Evaluate(interpreter);
        var rightVal = Right.Evaluate(interpreter);

        return Operator switch
        {
            TokenType.Plus => Add(leftVal, rightVal),
            TokenType.Minus => Subtract(leftVal, rightVal),
            TokenType.Multiply => Multiply(leftVal, rightVal),
            TokenType.Divide => Divide(leftVal, rightVal),
            TokenType.EqualsEquals => leftVal.Equals(rightVal),
            TokenType.NotEquals => !leftVal.Equals(rightVal),
            TokenType.LessThan => CompareLessThan(leftVal, rightVal),
            TokenType.GreaterThan => CompareGreaterThan(leftVal, rightVal),
            TokenType.And => (bool)leftVal && (bool)rightVal,
            TokenType.Or => (bool)leftVal || (bool)rightVal,
            _ => throw new Exception($"Unknown operator: {Operator}")
        };
    }

    private object Add(object left, object right)
    {
        if (left is int l && right is int r) return l + r;
        if (left is double || right is double) return Convert.ToDouble(left) + Convert.ToDouble(right);
        if (left is string || right is string) return left.ToString() + right.ToString();
        throw new Exception("Invalid operation Add");
    }

    private object Subtract(object left, object right)
    {
        if (left is int l && right is int r) return l - r;
        if (left is double || right is double) return Convert.ToDouble(left) - Convert.ToDouble(right);
        throw new Exception("Invalid operation Subtract");
    }

    private object Multiply(object left, object right)
    {
        if (left is int l && right is int r) return l * r;
        if (left is double || right is double) return Convert.ToDouble(left) * Convert.ToDouble(right);
        throw new Exception("Invalid operation Multiply");
    }

    private object Divide(object left, object right)
    {
        if (left is int l && right is int r) return l / r;
        if (left is double || right is double) return Convert.ToDouble(left) / Convert.ToDouble(right);
        throw new Exception("Invalid operation Divide");
    }

    private bool CompareLessThan(object left, object right)
    {
        if (left is int li && right is int ri) return li < ri;
        if (left is double ld && right is double rd) return ld < rd;
        throw new Exception("Comparison requires numbers");
    }

    private bool CompareGreaterThan(object left, object right)
    {
        if (left is int li && right is int ri) return li > ri;
        if (left is double ld && right is double rd) return ld > rd;
        throw new Exception("Comparison requires numbers");
    }
}
