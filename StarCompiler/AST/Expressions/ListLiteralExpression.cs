namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de lista literal: [1, 2, 3].
/// List literal expression: [1, 2, 3].
/// </summary>
public class ListLiteralExpression : Expression
{
    public List<Expression> Elements { get; set; }

    public ListLiteralExpression(List<Expression> elements)
    {
        Elements = elements;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        var list = new List<object>();
        foreach (var elem in Elements)
        {
            list.Add(elem.Evaluate(interpreter));
        }
        return list;
    }
}
