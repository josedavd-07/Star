using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using StarCompiler.Lexer;
using StarCompiler.Parser;
using StarCompiler.Runtime;

/// <summary>
/// Provee metadata para un proyecto Star.
/// </summary>
public class StarProject
{
    public string language { get; set; } = "Star";
    public string version { get; set; } = "1.0.0";
    public string? project_name { get; set; }
    public string main { get; set; } = "src/Main.st";
}

/// <summary>
/// Punto de entrada del compilador Star.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        string command = args[0];

        switch (command)
        {
            case "--version":
            case "-v":
                Console.WriteLine("Star v1.0 🌟");
                break;
            case "help":
            case "-h":
            case "--help":
                ShowUsage();
                break;
            case "new":
                CreateNewProject(args);
                break;
            case "run":
                RunProgram(args);
                break;
            case "build":
                BuildProgram(args);
                break;
            case "uninstall":
                Uninstall();
                break;
            default:
                Console.WriteLine($"[!] Comando desconocido: {command}");
                ShowUsage();
                break;
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("\x1b[1mStar Language Compiler 🌟\x1b[0m");
        Console.WriteLine("\nUso:");
        Console.WriteLine("  \x1b[32mstar new <name>\x1b[0m                - Crea una nueva constelación (proyecto)");
        Console.WriteLine("  \x1b[32mstar run [file.st]\x1b[0m             - Lanza la misión (ejecuta el script)");
        Console.WriteLine("  \x1b[32mstar build [file.st]\x1b[0m           - Construye el ejecutable para despliegue");
        Console.WriteLine("  \x1b[32mstar help\x1b[0m                      - Muestra esta guía de navegación");
        Console.WriteLine("  \x1b[32mstar --version\x1b[0m                 - Muestra la versión actual");
        Console.WriteLine("  \x1b[32mstar uninstall\x1b[0m                 - Elimina Star de este sistema");
    }

    static string? GetProjectEntry(string[] args)
    {
        // Si se provee un archivo, usar ese.
        if (args.Length >= 2 && args[1].EndsWith(".st"))
        {
            return args[1];
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

    static void RunProgram(string[] args)
    {
        string? filePath = GetProjectEntry(args);

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("[!] Error: No se encontró un archivo de entrada o .starproj.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[!] Error: Archivo no encontrado: {filePath}");
            return;
        }

        string code = File.ReadAllText(filePath);

        try
        {
            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();
            var parser = new ASTParser(tokens);
            var statements = parser.Parse();
            var interpreter = new Interpreter();
            interpreter.ExecuteWithMainSupport(statements);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error de ejecución: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void BuildProgram(string[] args)
    {
        string? filePath = GetProjectEntry(args);

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("[!] Error: No se encontró un archivo de entrada o .starproj.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[!] Error: Archivo no encontrado: {filePath}");
            return;
        }

        Console.WriteLine($"[*] Preparando despegue para {filePath}...");

        try
        {
            string absolutePath = Path.GetFullPath(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release");
            string outputPath = Path.Combine(outputDir, fileName);

            Directory.CreateDirectory(outputDir);

            // Validación rápida
            string code = File.ReadAllText(filePath);
            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();
            var parser = new ASTParser(tokens);
            parser.Parse();

            Console.WriteLine("[+] Validación de trayectoria (sintaxis) completada.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish -c Release -r linux-x64 --self-contained -o \"{outputDir}\"",
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("[!] Error: No se pudo iniciar el proceso de construcción.");
                    return;
                }

                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("[!] ¡Fallo en el sistema! La construcción ha fallado.");
                    Console.WriteLine(error);
                    return;
                }
            }

            Console.WriteLine($"[+] ¡Nave construida con éxito!");
            Console.WriteLine($"[*] Ubicación: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error crítico durante la construcción: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void CreateNewProject(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("[!] Error: Nombre de proyecto requerido. Uso: star new <nombre>");
            return;
        }

        string projectName = args[1];

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

            Directory.CreateDirectory(projectName);
            Directory.CreateDirectory(Path.Combine(projectName, "src"));

            // Generar .starproj
            var projectMetadata = new StarProject { project_name = projectName };
            string projJson = JsonSerializer.Serialize(projectMetadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectName, $"{projectName}.starproj"), projJson);
            Console.WriteLine($"[Star] 📄 Archivo de configuración .starproj generado.");

            // Generar Main.st con una plantilla más funcional
            string mainFile = Path.Combine(projectName, "src", "Main.st");
            string template = $@"StarName {projectName}.Core;

// Ejemplo de una Constelación (Clase)
Constellation Explorador {{
    Public String Nombre;
    Public Int Nivel;

    Public StarFunction Saludar() {{
        EmitLn(""¡Hola! Soy "" + this.Nombre + "" y mi nivel es "" + this.Nivel);
    }}
}}

StarFunction Main() {{
    EmitLn(""--- ¡Bienvenido a la Galaxia {projectName}! ---"");
    
    // Instanciando un explorador
    Explorador p = new Explorador();
    p.Nombre = ""Nova"";
    p.Nivel = 10;
    p.Saludar();

    EmitLn(""Tu aventura galáctica comienza ahora."");
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
}