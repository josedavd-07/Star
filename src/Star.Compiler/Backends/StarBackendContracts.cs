using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Ir;

namespace Star.Compiler.Backends;

/// <summary>Capabilities that a target backend may explicitly advertise.</summary>
[Flags]
public enum StarBackendCapabilities
{
    None = 0,
    DebugInformation = 1,
    ExecutableArtifact = 2
}

/// <summary>Stable metadata used to select a backend without loading or invoking it.</summary>
public sealed record StarBackendDescriptor
{
    public StarBackendDescriptor(string id, string displayName, StarBackendCapabilities capabilities)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A backend identifier is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A backend display name is required.", nameof(displayName));
        Id = id;
        DisplayName = displayName;
        Capabilities = capabilities;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public StarBackendCapabilities Capabilities { get; }
}

/// <summary>An immutable artifact produced by a backend. Persisting it is the caller's responsibility.</summary>
public sealed record StarBackendArtifact
{
    public StarBackendArtifact(string name, string contentType, ImmutableArray<byte> content)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An artifact name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("An artifact content type is required.", nameof(contentType));
        Name = name;
        ContentType = contentType;
        Content = content.IsDefault ? ImmutableArray<byte>.Empty : content;
    }

    public string Name { get; }
    public string ContentType { get; }
    public ImmutableArray<byte> Content { get; }
}

/// <summary>Result of backend emission, kept separate from file-system and process side effects.</summary>
public sealed record StarBackendEmission(
    ImmutableArray<StarBackendArtifact> Artifacts,
    ImmutableArray<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

/// <summary>
/// Extension point for platform-specific Star targets.
/// Implementations consume lowered IR and return data only; the compiler host owns plugin discovery and output I/O.
/// </summary>
public interface IStarBackend
{
    StarBackendDescriptor Descriptor { get; }

    StarBackendEmission Emit(StarIrCompilationUnit compilation);
}
