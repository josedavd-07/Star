namespace StarCompiler.AST;

/// <summary>
/// Nodo base del AST (Abstract Syntax Tree).
/// Base node for the AST.
/// </summary>
public abstract class ASTNode
{
    public int Line { get; set; }
    public int Column { get; set; }
}

/// <summary>
/// Nodo de expresión - produce un valor.
/// Expression node - produces a value.
/// </summary>
public abstract class Expression : ASTNode
{
    public abstract object Evaluate(Runtime.Interpreter interpreter);
}

/// <summary>
/// Nodo de declaración - ejecuta una acción.
/// Statement node - executes an action.
/// </summary>
public abstract class Statement : ASTNode
{
    public abstract void Execute(Runtime.Interpreter interpreter);
}
