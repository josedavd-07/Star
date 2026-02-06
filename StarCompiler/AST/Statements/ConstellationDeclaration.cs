using StarCompiler.AST.Statements;

namespace StarCompiler.AST.Statements;

/// <summary>
/// Representa una declaración de Constellation (Clase).
/// Represents a Constellation (Class) declaration.
/// </summary>
public class ConstellationDeclaration : Statement
{
    public string Name { get; set; }
    public List<VariableDeclaration> Fields { get; set; }
    public List<FunctionDeclaration> Methods { get; set; }
    public Accessibility Accessibility { get; set; }

    public ConstellationDeclaration(string name, Accessibility access = Accessibility.Private)
    {
        Name = name;
        Fields = new List<VariableDeclaration>();
        Methods = new List<FunctionDeclaration>();
        Accessibility = access;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        interpreter.DeclareConstellation(this);
    }
}
