namespace StarCompiler.AST.Statements;

/// <summary>
/// Representa una declaración de espacio de nombres: StarName Andromeda.System;
/// Represents a namespace declaration: StarName Andromeda.System;
/// </summary>
public class NamespaceDeclaration : Statement
{
    public string Name { get; set; }

    public NamespaceDeclaration(string name)
    {
        Name = name;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        interpreter.SetCurrentNamespace(Name);
    }
}
