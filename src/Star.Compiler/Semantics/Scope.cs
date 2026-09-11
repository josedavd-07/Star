using System.Collections.Immutable;

namespace Star.Compiler.Semantics;

/// <summary>A lexical scope. Lookup walks only its parent chain.</summary>
public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.Ordinal);
    public Scope(Scope? parent = null) => Parent = parent;
    public Scope? Parent { get; }
    public ImmutableArray<Symbol> Symbols => _symbols.Values.OrderBy(symbol => symbol.Name, StringComparer.Ordinal).ToImmutableArray();
    public bool TryDeclare(Symbol symbol) => _symbols.TryAdd(symbol.Name, symbol);
    public bool TryLookup(string name, out Symbol? symbol)
    {
        for (Scope? scope = this; scope is not null; scope = scope.Parent)
            if (scope._symbols.TryGetValue(name, out var found)) { symbol = found; return true; }
        symbol = null;
        return false;
    }
}
