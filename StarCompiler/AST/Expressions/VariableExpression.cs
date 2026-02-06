namespace StarCompiler.AST.Expressions;

/// <summary>
/// Expresión de variable: referencia a una variable por nombre.
/// Variable expression: reference to a variable by name.
/// </summary>
public class VariableExpression : Expression
{
    public string Name { get; set; }

    public VariableExpression(string name)
    {
        Name = name;
    }

    public override object Evaluate(Runtime.Interpreter interpreter)
    {
        return interpreter.GetVariable(Name);
    }
}
