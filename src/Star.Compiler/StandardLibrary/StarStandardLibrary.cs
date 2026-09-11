using System.Collections.Immutable;

namespace Star.Compiler.StandardLibrary;

/// <summary>
/// Describes the platform-neutral surface of Star's core library.
/// These are language contracts only: neither this class nor the front end executes them.
/// </summary>
public static class StarStandardLibrary
{
    private static readonly ImmutableArray<StarStandardType> CoreTypes =
    [
        new("Int"),
        new("String"),
        new("Double"),
        new("Float"),
        new("Char"),
        new("Bool"),
        new("Nova"),
        new("Object"),
        new("Galaxy"),
        new("Auto")
    ];

    private static readonly ImmutableArray<StarStandardFunction> CoreFunctions =
    [
        new("Length", "Int", ["Galaxy"]),
        new("Contains", "Bool", ["Galaxy", "Object"]),
        new("ToText", "String", ["Object"])
    ];

    /// <summary>The stable core contract shared by the semantic and type-analysis stages.</summary>
    public static StarStandardLibraryContract Core { get; } = new(CoreTypes, CoreFunctions);
}

/// <summary>An immutable, inspectable description of library names available to a Star program.</summary>
public sealed class StarStandardLibraryContract
{
    public StarStandardLibraryContract(
        ImmutableArray<StarStandardType> types,
        ImmutableArray<StarStandardFunction> functions)
    {
        Types = types.IsDefault ? ImmutableArray<StarStandardType>.Empty : types;
        Functions = functions.IsDefault ? ImmutableArray<StarStandardFunction>.Empty : functions;
        ValidateUniqueNames(Types.Select(type => type.Name), "type");
        ValidateUniqueNames(Functions.Select(function => function.Name), "function");
        ValidateFunctionTypes(Types, Functions);
    }

    public ImmutableArray<StarStandardType> Types { get; }
    public ImmutableArray<StarStandardFunction> Functions { get; }

    public bool IsType(string name) => Types.Any(type => string.Equals(type.Name, name, StringComparison.Ordinal));

    public bool TryGetFunction(string name, out StarStandardFunction? function)
    {
        function = Functions.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
        return function is not null;
    }

    private static void ValidateUniqueNames(IEnumerable<string> names, string kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"A standard-library {kind} must have a name.", nameof(names));
            if (!seen.Add(name)) throw new ArgumentException($"The standard-library {kind} '{name}' is declared more than once.", nameof(names));
        }
    }

    private static void ValidateFunctionTypes(
        ImmutableArray<StarStandardType> types,
        ImmutableArray<StarStandardFunction> functions)
    {
        var knownTypes = types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var function in functions)
        {
            if (!knownTypes.Contains(function.ReturnType))
                throw new ArgumentException($"The return type '{function.ReturnType}' of '{function.Name}' is not declared by this contract.", nameof(functions));
            foreach (var parameterType in function.ParameterTypes)
            {
                if (!knownTypes.Contains(parameterType))
                    throw new ArgumentException($"The parameter type '{parameterType}' of '{function.Name}' is not declared by this contract.", nameof(functions));
            }
        }
    }
}

/// <summary>A named Star type supplied by a standard-library contract.</summary>
public sealed record StarStandardType
{
    public StarStandardType(string name) => Name = ValidateName(name);

    public string Name { get; }

    private static string ValidateName(string name) => !string.IsNullOrWhiteSpace(name)
        ? name
        : throw new ArgumentException("A standard-library type must have a name.", nameof(name));
}

/// <summary>A platform-neutral function signature; it deliberately has no implementation.</summary>
public sealed record StarStandardFunction
{
    public StarStandardFunction(string name, string returnType, params string[] parameterTypes)
    {
        ArgumentNullException.ThrowIfNull(parameterTypes);
        Name = ValidateName(name);
        ReturnType = ValidateName(returnType);
        ParameterTypes = parameterTypes.ToImmutableArray();
        if (ParameterTypes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A standard-library parameter type must have a name.", nameof(parameterTypes));
    }

    public string Name { get; }
    public string ReturnType { get; }
    public ImmutableArray<string> ParameterTypes { get; }

    private static string ValidateName(string name) => !string.IsNullOrWhiteSpace(name)
        ? name
        : throw new ArgumentException("A standard-library function name and return type are required.", nameof(name));
}
