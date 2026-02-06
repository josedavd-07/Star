namespace StarCompiler.AST.Statements;

/// <summary>
/// Declaración return: return value.
/// Return statement: return value.
/// </summary>
public class ReturnStatement : Statement
{
    public Expression? Value { get; set; }

    public ReturnStatement(Expression? value = null)
    {
        Value = value;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        object? returnValue = Value?.Evaluate(interpreter);
        interpreter.SetReturnValue(returnValue);
    }
}
