namespace StarCompiler.AST.Statements;

/// <summary>
/// Asignación: x = 10.
/// Assignment: x = 10.
/// </summary>
public class AssignmentStatement : Statement
{
    public string Name { get; set; }
    public Expression Value { get; set; }

    public AssignmentStatement(string name, Expression value)
    {
        Name = name;
        Value = value;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        object val = Value.Evaluate(interpreter);
        interpreter.AssignVariable(Name, val);
    }
}
