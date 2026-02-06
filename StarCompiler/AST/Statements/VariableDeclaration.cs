namespace StarCompiler.AST.Statements;

/// <summary>
/// Declaración de variable: Int x = 5.
/// Variable declaration: Int x = 5.
/// </summary>
public class VariableDeclaration : Statement
{
    public string Type { get; set; }
    public string Name { get; set; }
    public Expression Initializer { get; set; }
    public Accessibility Accessibility { get; set; } = Accessibility.Public;
    public bool IsStatic { get; set; } = false;

    public VariableDeclaration(
        string type,
        string name,
        Expression initializer,
        Accessibility access = Accessibility.Public,
        bool isStatic = false)
    {
        Type = type;
        Name = name;
        Initializer = initializer;
        Accessibility = access;
        IsStatic = isStatic;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        object value = Initializer.Evaluate(interpreter);
        interpreter.DeclareVariable(Type, Name, value);
    }
}
