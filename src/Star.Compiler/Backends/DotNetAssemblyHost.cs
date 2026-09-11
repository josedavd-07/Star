using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Star.Compiler.Diagnostics;

namespace Star.Compiler.Backends;

/// <summary>
/// Host-owned final step for the managed backend. It deliberately sits outside IStarBackend:
/// emitting IR is pure, while this class explicitly owns file-system and process side effects.
/// </summary>
public sealed class DotNetAssemblyHost
{
    private const string GeneratedProjectName = "StarProgram";

    public StarBackendEmission Build(StarBackendEmission sourceEmission, string outputDirectory, string assemblyName, bool selfContained = false, string? runtimeIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(sourceEmission);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyName);
        if (sourceEmission.HasErrors) return sourceEmission;

        var source = sourceEmission.Artifacts.SingleOrDefault(artifact => artifact.Name == "Program.cs");
        if (source is null)
            return Failure("The managed .NET host requires the Program.cs artifact emitted by DotNetSourceBackend.");

        try
        {
            var destination = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(destination);
            // The SDK treats the output directory as part of the project's file set. Keep its
            // generated source elsewhere so an output artifact can never be compiled as input.
            var staging = Path.Combine(Path.GetTempPath(), "star-build", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            File.WriteAllBytes(Path.Combine(staging, "Program.cs"), source.Content.ToArray());
            File.WriteAllText(Path.Combine(staging, $"{GeneratedProjectName}.csproj"), ProjectFile(assemblyName), Encoding.UTF8);

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = staging,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(selfContained ? "publish" : "build");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("Release");
            startInfo.ArgumentList.Add("--nologo");
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(destination);
            if (selfContained)
            {
                startInfo.ArgumentList.Add("--self-contained");
                startInfo.ArgumentList.Add("true");
                startInfo.ArgumentList.Add("--runtime");
                startInfo.ArgumentList.Add(runtimeIdentifier ?? RuntimeInformation.RuntimeIdentifier);
            }

            using var process = Process.Start(startInfo);
            if (process is null) return Failure("Unable to start the .NET SDK process.");
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var detail = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                return Failure($"The .NET SDK could not build the generated Star program: {detail.Trim()}");
            }

            var generatedAssembly = Path.Combine(destination, $"{assemblyName}.dll");
            if (!File.Exists(generatedAssembly)) return Failure("The .NET SDK completed without producing the expected assembly.");
            return Artifacts(destination, assemblyName, selfContained);
        }
        catch (Exception exception)
        {
            return Failure($"The managed .NET host failed: {exception.Message}");
        }
    }

    private static StarBackendEmission Artifacts(string directory, string assemblyName, bool selfContained)
    {
        var names = new[] { $"{assemblyName}.dll", $"{assemblyName}.deps.json", $"{assemblyName}.runtimeconfig.json", assemblyName, $"{assemblyName}.exe" };
        var artifacts = names
            .Select(name => Path.Combine(directory, name))
            .Where(File.Exists)
            .Select(path => new StarBackendArtifact(Path.GetFileName(path), ContentType(path), File.ReadAllBytes(path).ToImmutableArray()))
            .ToImmutableArray();
        return new StarBackendEmission(artifacts, ImmutableArray<Diagnostic>.Empty);
    }

    private static StarBackendEmission Failure(string message) => new(
        ImmutableArray<StarBackendArtifact>.Empty,
        [new Diagnostic("STR6002", DiagnosticSeverity.Error, message)]);

    private static string ContentType(string path) => path.EndsWith(".dll", StringComparison.Ordinal) ? "application/vnd.microsoft.portable-executable" : "application/json";

    private static string ProjectFile(string assemblyName) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <AssemblyName>{{assemblyName}}</AssemblyName>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;
}
