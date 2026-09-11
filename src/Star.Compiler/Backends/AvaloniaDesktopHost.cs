using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Star.Compiler.Diagnostics;

namespace Star.Compiler.Backends;

/// <summary>
/// Builds a native desktop shell around the C# emitted by the managed Star backend.
/// The shell is intentionally small: Star owns program semantics, while Avalonia owns windows.
/// </summary>
public sealed class AvaloniaDesktopHost
{
    private const string ProjectName = "StarDesktop";

    public StarBackendEmission Build(StarBackendEmission sourceEmission, string outputDirectory, string applicationName, bool selfContained = false, string? runtimeIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(sourceEmission);
        if (sourceEmission.HasErrors) return sourceEmission;
        var program = sourceEmission.Artifacts.SingleOrDefault(artifact => artifact.Name == "Program.cs");
        if (program is null) return Failure("The desktop host requires the Program.cs artifact emitted by DotNetSourceBackend.");

        try
        {
            var destination = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(destination);
            var staging = Path.Combine(Path.GetTempPath(), "star-desktop-build", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            File.WriteAllBytes(Path.Combine(staging, "StarProgram.cs"), program.Content.ToArray());
            File.WriteAllText(Path.Combine(staging, "Desktop.cs"), DesktopSource(applicationName), Encoding.UTF8);
            File.WriteAllText(Path.Combine(staging, $"{ProjectName}.csproj"), ProjectFile(applicationName), Encoding.UTF8);

            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = staging, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(selfContained ? "publish" : "build"); start.ArgumentList.Add("-c"); start.ArgumentList.Add("Release"); start.ArgumentList.Add("--nologo"); start.ArgumentList.Add("--output"); start.ArgumentList.Add(destination);
            if (selfContained) { start.ArgumentList.Add("--self-contained"); start.ArgumentList.Add("true"); start.ArgumentList.Add("--runtime"); start.ArgumentList.Add(runtimeIdentifier ?? RuntimeInformation.RuntimeIdentifier); }
            using var process = Process.Start(start);
            if (process is null) return Failure("Unable to start the .NET SDK process for the desktop target.");
            var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
            if (process.ExitCode != 0) return Failure($"The desktop target could not build: {(string.IsNullOrWhiteSpace(error) ? output : error).Trim()}");

            var artifacts = Directory.GetFiles(destination).Select(path => new StarBackendArtifact(Path.GetFileName(path), ContentType(path), File.ReadAllBytes(path).ToImmutableArray())).ToImmutableArray();
            return new StarBackendEmission(artifacts, []);
        }
        catch (Exception exception) { return Failure($"The desktop target failed: {exception.Message}"); }
    }

    private static string DesktopSource(string title) => $$"""
        using System;
        using System.IO;
        using Avalonia;
        using Avalonia.Controls;
        using Avalonia.Controls.ApplicationLifetimes;

        internal sealed class StarDesktopApplication : Application
        {
            public override void OnFrameworkInitializationCompleted()
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var text = new StringWriter();
                    Console.SetOut(text);
                    StarProgram.Main();
                    desktop.MainWindow = new Window
                    {
                        Title = "{{title.Replace("\"", "\\\"", StringComparison.Ordinal)}}",
                        Width = 900,
                        Height = 600,
                        Content = new TextBox { Text = text.ToString(), IsReadOnly = true, AcceptsReturn = true, FontFamily = "Consolas", FontSize = 16, Padding = new Thickness(20) }
                    };
                }
                base.OnFrameworkInitializationCompleted();
            }
        }

        internal static class DesktopEntryPoint
        {
            public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<StarDesktopApplication>().UsePlatformDetect();
        }
        """;

    private static string ProjectFile(string applicationName) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <StartupObject>DesktopEntryPoint</StartupObject>
            <AssemblyName>{{applicationName}}</AssemblyName>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Avalonia.Desktop" Version="12.*" />
          </ItemGroup>
        </Project>
        """;
    private static StarBackendEmission Failure(string message) => new([], [new Diagnostic("STR8001", DiagnosticSeverity.Error, message)]);
    private static string ContentType(string path) => path.EndsWith(".dll", StringComparison.Ordinal) ? "application/vnd.microsoft.portable-executable" : "application/json";
}
