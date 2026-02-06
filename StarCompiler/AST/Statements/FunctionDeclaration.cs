namespace StarCompiler.AST.Statements;

/// <summary>
/// Parámetro de función.
/// Function parameter.
/// </summary>
public class Parameter
{
    public string Type { get; set; }
    public string Name { get; set; }

    public Parameter(string type, string name)
    {
        Type = type;
        Name = name;
    }
}

/// <summary>
/// Declaración de función: StarFunction name(params) { ... }.
/// Function declaration: StarFunction name(params) { ... }.
/// </summary>
public class FunctionDeclaration : Statement
{
    public string Name { get; set; }
    public List<Parameter> Parameters { get; set; }
    public string? ReturnType { get; set; }
    public List<Statement> Body { get; set; }
    public Accessibility Accessibility { get; set; } = Accessibility.Public;
    public bool IsStatic { get; set; } = false;

    public FunctionDeclaration(
        string name,
        List<Parameter> parameters,
        string? returnType,
        List<Statement> body,
        Accessibility access = Accessibility.Public,
        bool isStatic = false)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
        Body = body;
        Accessibility = access;
        IsStatic = isStatic;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        interpreter.DeclareFunction(this);
    }
}
