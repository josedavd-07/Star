namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de instanciación: new Constellation(args)
/// Instantiation expression: new Constellation(args)
/// </summary>
public class NewExpression : Expression
{
    public string ConstellationName { get; set; }
    public List<Expression> Arguments { get; set; }

    public NewExpression(string constName, List<Expression> arguments)
    {
        ConstellationName = constName;
        Arguments = arguments;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        // Logic to create a new object instance
        // return interpreter.Instantiate(ConstellationName, Arguments);
        throw new System.NotImplementedException("Object instantiation not implemented yet");
    }
}
