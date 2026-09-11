using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Star.Compiler.Diagnostics;
using Star.Compiler.Compilation;
using Star.Compiler.Backends;
using Star.Compiler.Ir;
using Star.Compiler.Text;

/// <summary>
/// Provee metadata para un proyecto Star.
/// </summary>
public class StarProject
{
    public string language { get; set; } = "Star";
    public string version { get; set; } = "1.0.0";
    public string? project_name { get; set; }
    public string target { get; set; } = "console";
    public string main { get; set; } = "src/Main.st";
}

/// <summary>
/// Punto de entrada del compilador Star.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return 2;
        }

        string command = args[0];

        switch (command)
        {
            case "--version":
            case "-v":
                Console.WriteLine("Star v1.0 🌟");
                return 0;
            case "help":
            case "-h":
            case "--help":
                ShowUsage();
                return 0;
            case "new":
                CreateNewProject(args);
                return 0;
            case "run":
                return RunProgram(args);
            case "build":
                return BuildProgram(args);
            case "uninstall":
                Uninstall();
                return 0;
            default:
                Console.WriteLine($"[!] Comando desconocido: {command}");
                ShowUsage();
                return 2;
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("\x1b[1mStar Language Compiler 🌟\x1b[0m");
        Console.WriteLine("\nUso:");
        Console.WriteLine("  \x1b[32mstar new console <name>\x1b[0m        - Crea un proyecto de consola Star");
        Console.WriteLine("  \x1b[32mstar run [file.st]\x1b[0m             - Compila y ejecuta la misión");
        Console.WriteLine("  \x1b[32mstar build [file.st] [--self-contained] [--runtime <RID>]\x1b[0m");
        Console.WriteLine("                                           - Construye un artefacto de Star");
        Console.WriteLine("  \x1b[32mstar help\x1b[0m                      - Muestra esta guía de navegación");
        Console.WriteLine("  \x1b[32mstar --version\x1b[0m                 - Muestra la versión actual");
        Console.WriteLine("  \x1b[32mstar uninstall\x1b[0m                 - Elimina Star de este sistema");
    }

    static string? GetProjectEntry(string[] args)
    {
        // Si se provee un archivo, usar ese.
        var explicitFile = args.FirstOrDefault(argument => argument.EndsWith(".st", StringComparison.Ordinal));
        if (explicitFile is not null)
        {
            return explicitFile;
        }

        // Buscar archivo .starproj en el directorio actual.
        var projFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.starproj");
        if (projFiles.Length > 0)
        {
            try
            {
                string json = File.ReadAllText(projFiles[0]);
                var proj = JsonSerializer.Deserialize<StarProject>(json);
                if (proj != null && !string.IsNullOrEmpty(proj.main))
                {
                    return proj.main;
                }
            }
            catch { }
        }

        return null;
    }

    static int RunProgram(string[] args)
    {
        string? filePath = GetProjectEntry(args);

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("[!] Error: No se encontró un archivo de entrada o .starproj.");
            return 2;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[!] Error: Archivo no encontrado: {filePath}");
            return 2;
        }

        try
        {
            var compilation = StarCompilerFacade.CompileFile(filePath);
            if (ReportDiagnostics(compilation.Source, compilation.Diagnostics))
            {
                return 1;
            }
            if (GetProjectTarget(args) == "web")
            {
                var page = BuildWebArtifact(compilation, filePath);
                if (page is null) return 1;
                Console.WriteLine($"[+] Sitio web generado: {page}");
                return 0;
            }
            if (GetProjectTarget(args) == "desktop")
            {
                var desktop = BuildDesktopArtifact(compilation, filePath, "Debug", IsSelfContained(args), RuntimeIdentifier(args));
                if (desktop is null) return 1;
                var desktopStart = LaunchInfo(desktop, IsSelfContained(args));
                using var desktopProcess = Process.Start(desktopStart);
                if (desktopProcess is null) return 1;
                desktopProcess.WaitForExit();
                return desktopProcess.ExitCode;
            }
            var artifact = BuildArtifact(compilation, filePath, "Debug", IsSelfContained(args), RuntimeIdentifier(args));
            if (artifact is null) return 1;
            var startInfo = LaunchInfo(artifact, IsSelfContained(args));
            using var process = Process.Start(startInfo);
            if (process is null) return 1;
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error de ejecución: {ex.Message}");
            return 1;
        }
    }

    static int BuildProgram(string[] args)
    {
        string? filePath = GetProjectEntry(args);

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("[!] Error: No se encontró un archivo de entrada o .starproj.");
            return 2;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[!] Error: Archivo no encontrado: {filePath}");
            return 2;
        }

        Console.WriteLine($"[*] Preparando despegue para {filePath}...");

        try
        {
            var compilation = StarCompilerFacade.CompileFile(filePath);
            if (ReportDiagnostics(compilation.Source, compilation.Diagnostics))
            {
                return 1;
            }
            if (GetProjectTarget(args) == "web")
            {
                var page = BuildWebArtifact(compilation, filePath);
                if (page is null) return 1;
                Console.WriteLine($"[+] Sitio web construido: {page}");
                return 0;
            }
            if (GetProjectTarget(args) == "desktop")
            {
                var desktop = BuildDesktopArtifact(compilation, filePath, "Release", IsSelfContained(args), RuntimeIdentifier(args));
                if (desktop is null) return 1;
                Console.WriteLine($"[+] Aplicación de escritorio construida: {desktop}");
                return 0;
            }
            var artifact = BuildArtifact(compilation, filePath, "Release", IsSelfContained(args), RuntimeIdentifier(args));
            if (artifact is null) return 1;

            Console.WriteLine($"[+] ¡Nave construida con éxito!");
            Console.WriteLine($"[*] Ubicación: {artifact}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error crítico durante la construcción: {ex.Message}");
            return 1;
        }
    }

    static string? BuildArtifact(CompilationResult compilation, string filePath, string configuration, bool selfContained = false, string? runtimeIdentifier = null)
    {
        var lowered = StarIrLowerer.Lower(compilation);
        if (ReportDiagnostics(compilation.Source, lowered.Diagnostics)) return null;
        var emitted = new DotNetSourceBackend().Emit(lowered.Ir!);
        if (ReportDiagnostics(compilation.Source, emitted.Diagnostics)) return null;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var outputDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filePath))!, "bin", configuration, name);
        var built = new DotNetAssemblyHost().Build(emitted, outputDirectory, name, selfContained, runtimeIdentifier);
        if (ReportDiagnostics(compilation.Source, built.Diagnostics)) return null;
        return ArtifactPath(outputDirectory, name, selfContained, built);
    }

    static string? BuildWebArtifact(CompilationResult compilation, string filePath)
    {
        var lowered = StarIrLowerer.Lower(compilation);
        if (ReportDiagnostics(compilation.Source, lowered.Diagnostics)) return null;
        var emitted = new JavaScriptSourceBackend().Emit(lowered.Ir!);
        if (ReportDiagnostics(compilation.Source, emitted.Diagnostics)) return null;
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "bin", "web");
        Directory.CreateDirectory(directory);
        foreach (var artifact in emitted.Artifacts) File.WriteAllBytes(Path.Combine(directory, artifact.Name), artifact.Content.ToArray());
        return Path.Combine(directory, "index.html");
    }

    static string? BuildDesktopArtifact(CompilationResult compilation, string filePath, string configuration, bool selfContained = false, string? runtimeIdentifier = null)
    {
        var lowered = StarIrLowerer.Lower(compilation);
        if (ReportDiagnostics(compilation.Source, lowered.Diagnostics)) return null;
        var emitted = new DotNetSourceBackend().Emit(lowered.Ir!);
        if (ReportDiagnostics(compilation.Source, emitted.Diagnostics)) return null;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filePath))!, "bin", configuration, name);
        var built = new AvaloniaDesktopHost().Build(emitted, directory, name, selfContained, runtimeIdentifier);
        if (ReportDiagnostics(compilation.Source, built.Diagnostics)) return null;
        return ArtifactPath(directory, name, selfContained, built);
    }

    static string GetProjectTarget(string[] args)
    {
        if (args.Any(argument => argument.EndsWith(".st", StringComparison.Ordinal))) return "console";
        var project = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.starproj").FirstOrDefault();
        if (project is null) return "console";
        try { return JsonSerializer.Deserialize<StarProject>(File.ReadAllText(project))?.target ?? "console"; }
        catch { return "console"; }
    }

    static bool IsSelfContained(string[] args) => args.Contains("--self-contained", StringComparer.Ordinal);

    static string? RuntimeIdentifier(string[] args)
    {
        var index = Array.IndexOf(args, "--runtime");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    static string? ArtifactPath(string directory, string name, bool selfContained, StarBackendEmission emission)
    {
        if (selfContained)
        {
            var executable = emission.Artifacts.FirstOrDefault(artifact => artifact.Name is var candidate && (candidate == name || candidate == $"{name}.exe"));
            if (executable is not null) return Path.Combine(directory, executable.Name);
        }
        return emission.Artifacts.FirstOrDefault(artifact => artifact.Name == $"{name}.dll") is { } assembly ? Path.Combine(directory, assembly.Name) : null;
    }

    static ProcessStartInfo LaunchInfo(string artifact, bool selfContained)
    {
        if (selfContained) return new ProcessStartInfo(artifact) { UseShellExecute = false };
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        start.ArgumentList.Add(artifact);
        return start;
    }

    static void CreateNewProject(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("[!] Error: Nombre de proyecto requerido. Uso: star new console <nombre>");
            return;
        }

        var target = "console";
        string projectName = args[1];

        if (args.Length >= 3 && args[1] is "console" or "desktop" or "web")
        {
            target = args[1];
            projectName = args[2];
        }

        // Soporte para la sintaxis antigua por si acaso 'console -name <name>'
        if (args.Length >= 4 && args[1] == "console" && args[2] == "-name")
        {
            projectName = args[3];
        }
        if (Directory.Exists(projectName))
        {
            Console.WriteLine($"[!] Error: El sistema estelar '{projectName}' ya existe.");
            return;
        }

        try
        {
            Console.WriteLine($"[Star] ✨ Forjando nueva galaxia: {projectName}...");
            var projectDirectoryName = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectName));
            var namespaceName = ToStarIdentifier(projectDirectoryName);

            Directory.CreateDirectory(projectName);
            Directory.CreateDirectory(Path.Combine(projectName, "src"));

            // Generar .starproj
            var projectMetadata = new StarProject { project_name = projectDirectoryName, target = target };
            string projJson = JsonSerializer.Serialize(projectMetadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectName, $"{projectDirectoryName}.starproj"), projJson);
            Console.WriteLine($"[Star] 📄 Archivo de configuración .starproj generado.");

            // The console template exercises the modern compiled path without depending on the legacy interpreter.
            string mainFile = Path.Combine(projectName, "src", "Main.st");
            string template = $@"StarName {namespaceName}.Core;

StarFunction Main() {{
    EmitLn(""Bienvenido a Star: {projectDirectoryName}"");
}}
";
            File.WriteAllText(mainFile, template);
            Console.WriteLine("[+] Plantilla Main.st creada con éxito.");

            Console.WriteLine("[Star] ✅ ¡Listo para el despegue! Usa 'star run'.");
            Console.WriteLine($"Escribe 'cd {projectName} && star run' para empezar.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error al inicializar el proyecto: {ex.Message}");
        }
    }

    static void Uninstall()
    {
        Console.WriteLine("[*] Desmantelando estación Star...");
        Console.WriteLine("\n[!] Para completar la desinstalación en Linux, ejecuta:");
        Console.WriteLine("    sudo rm /usr/local/bin/star");
        Console.WriteLine("    rm -rf ~/.star-language");
        Console.WriteLine("\n[!] Importante: Si configuraste variables de entorno permanentes en ~/.bashrc o ~/.profile,");
        Console.WriteLine("    no olvides eliminar las líneas que contienen 'STAR_PATH' o referencias a Star.");
        Console.WriteLine("\n[!] Para desinstalar las fuentes:");
        Console.WriteLine("    rm ~/.local/share/fonts/SF-Mono-*");
        Console.WriteLine("    fc-cache -f -v");
        Console.WriteLine("\n[!] Star espera volverte a ver pronto. ¡Buen viaje, explorador!");
    }

    private static string ToStarIdentifier(string name)
    {
        var characters = name.Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
        var result = new string(characters);
        return string.IsNullOrEmpty(result) || char.IsDigit(result[0]) ? "StarProject" : result;
    }

    private static bool ReportDiagnostics(SourceText source, IReadOnlyList<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Console.Error.WriteLine(DiagnosticFormatter.Format(source, diagnostic));
        }

        return diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
