namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de acceso a miembro: objeto.estrella o objeto.sistema()
/// Member access expression: object.member or object.method()
/// </summary>
public class MemberAccessExpression : Expression
{
    public Expression Object { get; set; }
    public string MemberName { get; set; }

    public MemberAccessExpression(Expression obj, string memberName)
    {
        Object = obj;
        MemberName = memberName;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        // For now, let's handle static calls if Object is a VariableExpression that matches a Constellation name
        if (Object is VariableExpression varExpr)
        {
            if (interpreter.IsConstellation(varExpr.Name))
            {
                // Static access
                return interpreter.GetStaticMember(varExpr.Name, MemberName);
            }
        }

        // Instance access (Placeholder)
        object instance = Object.Evaluate(interpreter);
        throw new System.NotImplementedException($"Instance member access for '{MemberName}' not implemented yet.");
    }
}
