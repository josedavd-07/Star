namespace StarCompiler.AST.Statements;

/// <summary>
/// Declaración Emit: Emit(value) o EmitLn(value).
/// Emit statement: Emit(value) or EmitLn(value).
/// </summary>
public class EmitStatement : Statement
{
    public Expression Value { get; set; }
    public bool Newline { get; set; }

    public EmitStatement(Expression value, bool newline)
    {
        Value = value;
        Newline = newline;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        object val = Value.Evaluate(interpreter);
        interpreter.Emit(val, Newline);
    }
}
