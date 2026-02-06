namespace StarCompiler.AST.Statements;

/// <summary>
/// Bucle Foreach/Explore: Explore (var in collection) { ... }.
/// Foreach loop: Explore (var in collection) { ... }.
/// </summary>
public class ForeachStatement : Statement
{
    public string VarName { get; set; }
    public string CollectionName { get; set; }
    public List<Statement> Body { get; set; }

    public ForeachStatement(string varName, string collectionName, List<Statement> body)
    {
        VarName = varName;
        CollectionName = collectionName;
        Body = body;
    }

    public override void Execute(Runtime.Interpreter interpreter)
    {
        var collection = interpreter.GetVariable(CollectionName);

        if (collection is not List<object> list)
        {
            throw new Exception($"'{CollectionName}' is not a Galaxy (List)");
        }

        foreach (var item in list)
        {
            interpreter.DeclareVariable("Auto", VarName, item);

            foreach (var stmt in Body)
            {
                stmt.Execute(interpreter);
            }
        }
    }
}
