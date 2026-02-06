namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de llamada a función: greet("Alice").
/// Function call expression: greet("Alice").
/// </summary>
public class CallExpression : Expression
{
    public Expression Callee { get; set; }
    public List<Expression> Arguments { get; set; }

    public CallExpression(Expression callee, List<Expression> arguments)
    {
        Callee = callee;
        Arguments = arguments;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        var args = new List<object>();
        foreach (var arg in Arguments)
        {
            var val = arg.Evaluate(interpreter);
            if (val == null) throw new System.Exception("Argument evaluated to null");
            args.Add(val);
        }

        if (Callee is VariableExpression varExpr)
        {
            string funcName = varExpr.Name;
            if (funcName == "Emit" || funcName == "EmitLn" || funcName == "Log" || funcName == "Read")
            {
                if (funcName == "Read")
                    return Console.ReadLine() ?? "";

                object val = args.Count > 0 ? args[0] : "";
                if (funcName == "Emit")
                    Console.Write(val);
                else if (funcName == "EmitLn")
                    Console.WriteLine(val);
                else if (funcName == "Log")
                    Console.WriteLine($"LOG: {val}");
                return null; // These functions don't return a value in this context
            }
            return interpreter.CallFunction(varExpr.Name, args);
        }
        else if (Callee is MemberAccessExpression memberExpr)
        {
            var function = memberExpr.Evaluate(interpreter);
            if (function is Statements.FunctionDeclaration funcDecl)
            {
                return interpreter.CallFunction(funcDecl.Name, args);
            }
        }

        throw new System.Exception("Callee is not a callable function");
    }
}
