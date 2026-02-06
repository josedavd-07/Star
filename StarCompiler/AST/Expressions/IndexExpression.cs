namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de indexación: arr[0].
/// Index expression: arr[0].
/// </summary>
public class IndexExpression : Expression
{
    public Expression Target { get; set; }
    public Expression Index { get; set; }

    public IndexExpression(Expression target, Expression index)
    {
        Target = target;
        Index = index;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        var targetVal = Target.Evaluate(interpreter);
        var indexVal = Index.Evaluate(interpreter);

        if (targetVal is List<object> list)
        {
            int index = Convert.ToInt32(indexVal);
            return list[index];
        }

        throw new Exception("Target is not a Galaxy (List)");
    }
}
