using StarCompiler.AST;
using StarCompiler.AST.Statements;

namespace StarCompiler.Runtime;

/// <summary>
/// Intérprete que ejecuta el AST.
/// Interpreter that executes the AST.
/// </summary>
public class Interpreter
{
    private readonly StarRuntime _runtime;
    private readonly Dictionary<string, FunctionDeclaration> _functions;
    private readonly Dictionary<string, ConstellationDeclaration> _constellations;
    private string _currentNamespace = "";
    private object? _returnValue;
    private bool _hasReturned;

    public Interpreter()
    {
        _runtime = new StarRuntime();
        _functions = new Dictionary<string, FunctionDeclaration>();
        _constellations = new Dictionary<string, ConstellationDeclaration>();
        _returnValue = null;
        _hasReturned = false;
    }

    public void Execute(List<Statement> statements)
    {
        foreach (var stmt in statements)
        {
            if (_hasReturned) break;
            stmt.Execute(this);
        }
    }

    public void ExecuteWithMainSupport(List<Statement> statements)
    {
        // 1. First pass: Register all global declarations (Functions, Constellations, Namespaces)
        // And collect non-declaration statements for script-style execution.
        var topLevelStatements = new List<Statement>();

        foreach (var stmt in statements)
        {
            if (stmt is FunctionDeclaration || stmt is ConstellationDeclaration || stmt is NamespaceDeclaration)
            {
                stmt.Execute(this);
            }
            else
            {
                topLevelStatements.Add(stmt);
            }
        }

        // 2. Decide entry point
        if (_functions.ContainsKey("Main"))
        {
            // Execute Main
            CallFunction("Main", new List<object>());
        }
        else
        {
            // Execute as script
            Execute(topLevelStatements);
        }
    }

    // Variable management / Gestión de variables
    public void DeclareVariable(string type, string name, object value)
    {
        _runtime.DeclareVariable(type, name, value);
    }

    public void AssignVariable(string name, object value)
    {
        _runtime.DeclareVariable("Auto", name, value);
    }

    public object GetVariable(string name)
    {
        return _runtime.GetVariable(name);
    }

    // Namespace management
    public void SetCurrentNamespace(string ns)
    {
        _currentNamespace = ns;
    }

    // Constellation (Class) management
    public bool IsConstellation(string name)
    {
        return _constellations.ContainsKey(name);
    }

    public object GetStaticMember(string constName, string memberName)
    {
        if (!_constellations.TryGetValue(constName, out var constellation))
            throw new Exception($"Constellation '{constName}' not found");

        var method = constellation.Methods.FirstOrDefault(m => m.Name == memberName && m.IsStatic);
        if (method != null) return method;

        var field = constellation.Fields.FirstOrDefault(f => f.Name == memberName && f.IsStatic);
        if (field != null) return field;

        throw new Exception($"Static member '{memberName}' not found in constellation '{constName}'");
    }

    public void DeclareConstellation(ConstellationDeclaration constellation)
    {
        // For now, names are global, but could be ns + name
        _constellations[constellation.Name] = constellation;

        // Register static methods as global functions for now to support Main
        foreach (var method in constellation.Methods)
        {
            if (method.IsStatic)
            {
                DeclareFunction(method);
            }
        }
    }

    // Function management / Gestión de funciones
    public void DeclareFunction(FunctionDeclaration func)
    {
        _functions[func.Name] = func;
    }

    public object? CallFunction(string name, List<object> args)
    {
        if (!_functions.ContainsKey(name))
        {
            throw new Exception($"Function '{name}' not found");
        }

        var func = _functions[name];

        if (func.Parameters.Count != args.Count)
        {
            throw new Exception($"Function '{name}' expects {func.Parameters.Count} arguments, got {args.Count}");
        }

        // Push new scope (simple: save current variables)
        var savedVars = _runtime.SaveState();

        // Bind parameters
        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            _runtime.DeclareVariable(param.Type, param.Name, args[i]);
        }

        // Execute function body
        _hasReturned = false;
        _returnValue = null;

        foreach (var stmt in func.Body)
        {
            if (_hasReturned) break;
            stmt.Execute(this);
        }

        var result = _returnValue;

        // Pop scope (restore variables)
        _runtime.RestoreState(savedVars);

        _hasReturned = false;
        _returnValue = null;

        return result;
    }

    public void SetReturnValue(object? value)
    {
        _returnValue = value;
        _hasReturned = true;
    }

    // I/O
    public void Emit(object value, bool newline)
    {
        _runtime.Emit(value, newline);
    }
}
