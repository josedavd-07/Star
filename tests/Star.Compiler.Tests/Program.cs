using Star.Compiler.Diagnostics;
using Star.Compiler.Parsing;
using Star.Compiler.Syntax;
using Star.Compiler.Text;
using Star.Compiler.Semantics;
using Star.Compiler.Compilation;
using Star.Compiler.Backends;
using Star.Compiler.Ir;
using Star.Compiler.StandardLibrary;
using StarCompiler.Lexer;
using StarCompiler.Parser;
using StarCompiler.Runtime;
using StarCompiler.AST.Expressions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

var tests = new (string Name, Action Execute)[]
{
    ("SourceText preserves line and column locations", SourceTextPreservesLocations),
    ("DiagnosticBag preserves diagnostic metadata", DiagnosticBagPreservesMetadata),
    ("Lexer reports malformed source with a precise location", LexerReportsPreciseDiagnostic),
    ("Syntax parser builds a pure nested Star tree", SyntaxParserBuildsNestedTree),
    ("Syntax parser creates declaration nodes with typed headers", SyntaxParserCreatesDeclarationNodes),
    ("Syntax parser recognizes Constellation types in declarations", SyntaxParserRecognizesConstellationTypes),
    ("Syntax parser preserves expression precedence and postfix operations", SyntaxParserBuildsExpressionTree),
    ("Syntax parser creates explicit Orbit and SpinWhile nodes", SyntaxParserCreatesLoopNodes),
    ("Semantic analyzer resolves lexical scopes", SemanticAnalyzerResolvesLexicalScopes),
    ("Semantic analyzer reports duplicate names and unknown types", SemanticAnalyzerReportsDeclarationErrors),
    ("Type checker validates declarations, conditions, and returns", TypeCheckerValidatesCompatibleProgram),
    ("Type checker reports incompatible Star expressions", TypeCheckerReportsIncompatibleExpressions),
    ("Type checker resolves Constellation members and method calls", TypeCheckerResolvesConstellationMembers),
    ("Type checker reports invalid member access and arguments", TypeCheckerReportsMemberErrors),
    ("Compilation pipeline centralizes parser, symbols, and types", CompilationPipelineCentralizesFrontEnd),
    ("Compiler facade preserves file diagnostics for CLI clients", CompilerFacadePreservesFileDiagnostics),
    ("Standard library contracts are immutable and platform-neutral", StandardLibraryContractsArePlatformNeutral),
    ("Backend contracts isolate IR emission from output side effects", BackendContractsIsolateEmission),
    ("Managed .NET source backend emits control flow and reports unsupported nodes", DotNetSourceBackendEmitsSupportedIr),
    ("Managed .NET source backend emits Galaxy collections and Explore loops", DotNetSourceBackendEmitsExplore),
    ("Managed .NET source backend emits Constellation members and standard-library calls", DotNetSourceBackendEmitsObjectsAndLibrary),
    ("Constellation constructors validate arguments and emit managed constructors", ConstellationConstructorsCompile),
    ("CLI run returns stable results for valid, invalid, and invalid-input cases", CliRunReturnsStableResults),
    ("IR lowering preserves Star control flow, bindings, types, and spans", IrLoweringPreservesContracts),
    ("IR lowering retains diagnostics by refusing invalid compilations", IrLoweringRejectsInvalidCompilation),
    ("IR shape agrees with legacy execution for functions and control flow", IrAgreesWithLegacyExecution),
    ("IR shape agrees with legacy execution for assignment and Orbit", IrAgreesWithLegacyAssignmentAndOrbit),
    ("Legacy Nova calls return an explicit unit value", LegacyNovaCallsReturnUnit),
    ("Syntax parser reports unterminated blocks", SyntaxParserReportsUnterminatedBlocks),
    ("Legacy lexer recognizes Star control-flow tokens", LegacyLexerRecognizesSyntax),
    ("Legacy parser accepts a documented executable program", LegacyParserAcceptsExecutableProgram),
    ("Legacy runtime executes a documented basic program", LegacyRuntimeExecutesBasicProgram)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Execute();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {test.Name}: {exception.Message}");
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

return 0;

static void SourceTextPreservesLocations()
{
    var source = SourceText.From("Int fuel = 1;\r\nEmitLn(fuel);", "Mission.st");
    AssertEqual((0, 4), source.GetLineColumn(4));
    AssertEqual((1, 0), source.GetLineColumn(15));
    AssertEqual("fuel", source.ToString(new TextSpan(4, 4)));
}

static void DiagnosticBagPreservesMetadata()
{
    var diagnostics = new DiagnosticBag();
    diagnostics.ReportError("STR1001", new TextSpan(3, 1), "Unexpected character.");
    var diagnostic = diagnostics.ToImmutable().Single();

    AssertEqual(true, diagnostics.HasErrors);
    AssertEqual("STR1001", diagnostic.Code);
    AssertEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    AssertEqual<TextSpan?>(new TextSpan(3, 1), diagnostic.Span);
}

static void LexerReportsPreciseDiagnostic()
{
    var source = SourceText.From("Int fuel = @;", "Mission.st");
    var lexer = new Lexer(source);
    lexer.Tokenize();
    var diagnostic = lexer.Diagnostics.Single();

    AssertEqual("STR1001", diagnostic.Code);
    AssertEqual("Mission.st(1,12): error STR1001: Unexpected character '@'.", DiagnosticFormatter.Format(source, diagnostic));
}

static void SyntaxParserBuildsNestedTree()
{
    var result = StarParser.Parse(SourceText.From("StarFunction Main() { Int fuel = 1; When (fuel > 0) { EmitLn(\"launch\"); } }"));

    AssertEqual(false, result.HasErrors);
    AssertEqual(1, result.Root.Statements.Length);
    var function = AssertType<FunctionDeclarationSyntax>(result.Root.Statements[0]);
    AssertEqual(SyntaxKind.FunctionDeclaration, function.Kind);
    AssertEqual(2, function.Body.Statements.Length);
    AssertEqual(SyntaxKind.WhenStatement, function.Body.Statements[1].Kind);
}

static void SyntaxParserCreatesDeclarationNodes()
{
    var result = StarParser.Parse(SourceText.From("StarName Nova.Core; Constellation Ship { Public String name = \"Orion\"; Public StarFunction Launch(Int fuel) -> Nova { EmitLn(name); } }"));

    AssertEqual(false, result.HasErrors);
    var space = AssertType<NamespaceDeclarationSyntax>(result.Root.Statements[0]);
    AssertEqual("Nova", space.Name.Parts[0].Value);
    var constellation = AssertType<ConstellationDeclarationSyntax>(result.Root.Statements[1]);
    AssertEqual("Ship", constellation.Identifier.Value);
    var field = AssertType<VariableDeclarationSyntax>(constellation.Body.Statements[0]);
    AssertEqual("String", field.Type.Identifier.Value);
    AssertEqual("name", field.Identifier.Value);
    AssertType<LiteralExpressionSyntax>(field.Initializer!);
    var function = AssertType<FunctionDeclarationSyntax>(constellation.Body.Statements[1]);
    AssertEqual("Launch", function.Identifier.Value);
    AssertEqual("fuel", function.Parameters[0].Identifier.Value);
    AssertEqual("Nova", function.ReturnType!.Identifier.Value);
}

static void SyntaxParserRecognizesConstellationTypes()
{
    var result = StarParser.Parse(SourceText.From("Constellation Engine { } StarFunction Main() { Engine engine = new Engine(); }"));

    AssertEqual(false, result.HasErrors);
    var main = AssertType<FunctionDeclarationSyntax>(result.Root.Statements[1]);
    var declaration = AssertType<VariableDeclarationSyntax>(main.Body.Statements.Single());
    AssertEqual("Engine", declaration.Type.Identifier.Value);
    AssertEqual("engine", declaration.Identifier.Value);
}

static void SyntaxParserBuildsExpressionTree()
{
    var result = StarParser.Parse(SourceText.From("Int energy = ship.engine.Start(4 + 2 * 3);"));

    AssertEqual(false, result.HasErrors);
    var declaration = AssertType<VariableDeclarationSyntax>(result.Root.Statements.Single());
    var call = AssertType<CallExpressionSyntax>(declaration.Initializer!);
    AssertType<MemberAccessExpressionSyntax>(call.Target);
    var addition = AssertType<BinaryExpressionSyntax>(call.Arguments.Single());
    AssertEqual(Star.Compiler.Lexing.StarTokenKind.Plus, addition.OperatorToken.Kind);
    var multiplication = AssertType<BinaryExpressionSyntax>(addition.Right);
    AssertEqual(Star.Compiler.Lexing.StarTokenKind.Multiply, multiplication.OperatorToken.Kind);
}

static void SyntaxParserCreatesLoopNodes()
{
    var result = StarParser.Parse(SourceText.From("Orbit (Int index = 0; index < 3; index = index + 1) { EmitLn(index); } SpinWhile { EmitLn(\"once\"); } (true);"));

    AssertEqual(false, result.HasErrors);
    var orbit = AssertType<OrbitStatementSyntax>(result.Root.Statements[0]);
    AssertType<VariableDeclarationSyntax>(orbit.Initializer!);
    AssertType<BinaryExpressionSyntax>(orbit.Condition!);
    AssertType<AssignmentStatementSyntax>(orbit.Step!);
    AssertType<SpinWhileStatementSyntax>(result.Root.Statements[1]);
}

static void SemanticAnalyzerResolvesLexicalScopes()
{
    var parse = StarParser.Parse(SourceText.From("Constellation Ship { Public StarFunction Launch(Int fuel) -> Nova { When (fuel > 0) { Int thrust = fuel; EmitLn(thrust); } } }"));
    var result = StarSemanticAnalyzer.Analyze(parse.Root);

    AssertEqual(false, result.HasErrors);
    AssertEqual(SymbolKind.Constellation, result.GlobalScope.Symbols.Single(symbol => symbol.Name == "Ship").Kind);
}

static void SemanticAnalyzerReportsDeclarationErrors()
{
    var parse = StarParser.Parse(SourceText.From("Int first = new Missing(); Int first = missing;"));
    var result = StarSemanticAnalyzer.Analyze(parse.Root);

    AssertEqual(true, result.HasErrors);
    AssertEqual("STR3003", result.Diagnostics[0].Code);
    AssertEqual("STR3002", result.Diagnostics[1].Code);
    AssertEqual("STR3001", result.Diagnostics[2].Code);
}

static void TypeCheckerValidatesCompatibleProgram()
{
    var parse = StarParser.Parse(SourceText.From("StarFunction Launch(Int fuel) -> Int { When (fuel > 0) { Int result = fuel + 1; return result; } return 0; }"));
    var result = StarTypeChecker.Check(parse.Root);

    AssertEqual(false, result.HasErrors);
}

static void TypeCheckerReportsIncompatibleExpressions()
{
    var parse = StarParser.Parse(SourceText.From("StarFunction Launch() -> Int { Int fuel = \"full\"; When (fuel) { return \"no\"; } return 0; }"));
    var result = StarTypeChecker.Check(parse.Root);

    AssertEqual(true, result.HasErrors);
    AssertEqual("STR4003", result.Diagnostics[0].Code);
    AssertEqual("STR4002", result.Diagnostics[1].Code);
    AssertEqual("STR4003", result.Diagnostics[2].Code);
}

static void TypeCheckerResolvesConstellationMembers()
{
    var parse = StarParser.Parse(SourceText.From("Constellation Engine { Public Int fuel; Public StarFunction Ignite(Int amount) -> Int { return amount; } } StarFunction Main() { Engine engine = new Engine(); Int power = engine.Ignite(5); engine.fuel = power; }"));
    var result = StarTypeChecker.Check(parse.Root);

    AssertEqual(false, result.HasErrors);
}

static void TypeCheckerReportsMemberErrors()
{
    var parse = StarParser.Parse(SourceText.From("Constellation Engine { Public StarFunction Ignite(Int amount) -> Int { return amount; } Private Int secret = 1; } StarFunction Main() { Engine engine = new Engine(); engine.Ignite(\"bad\"); engine.secret; engine.Unknown(); }"));
    var result = StarTypeChecker.Check(parse.Root);

    AssertEqual(true, result.HasErrors);
    AssertEqual("STR4003", result.Diagnostics[0].Code);
    AssertEqual("STR5002", result.Diagnostics[1].Code);
    AssertEqual("STR5001", result.Diagnostics[2].Code);
}

static void CompilationPipelineCentralizesFrontEnd()
{
    var source = SourceText.From("Constellation Engine { Public StarFunction Ignite(Int amount) -> Int { return amount; } } StarFunction Main() { Engine engine = new Engine(); Int power = engine.Ignite(3); }");
    var compilation = StarCompilation.Create(source);

    AssertEqual(false, compilation.HasErrors);
    AssertEqual(source, compilation.Source);
    AssertEqual(2, compilation.Syntax.Statements.Length);
    AssertEqual(true, compilation.Semantics.GlobalScope.Symbols.Any(symbol => symbol.Name == "Engine"));
    var main = AssertType<FunctionDeclarationSyntax>(compilation.Syntax.Statements[1]);
    var declaration = AssertType<VariableDeclarationSyntax>(main.Body.Statements[0]);
    AssertEqual("Engine", compilation.Types.ExpressionTypes[declaration.Initializer!]);
    AssertEqual(SymbolKind.Variable, compilation.Semantics.Symbols[declaration].Kind);
}

static void CompilerFacadePreservesFileDiagnostics()
{
    var compilation = StarCompilerFacade.CompileText("Int fuel = @;", "Mission.st");

    AssertEqual(true, compilation.HasErrors);
    AssertEqual(true, StarCompilerFacade.FormatDiagnostics(compilation).Contains("Mission.st(1,12): error STR1001: Unexpected character '@'."));
}

static void StandardLibraryContractsArePlatformNeutral()
{
    var library = StarStandardLibrary.Core;

    AssertEqual(true, library.IsType("Galaxy"));
    AssertEqual(true, library.TryGetFunction("Length", out var length));
    AssertEqual("Int", length!.ReturnType);
    AssertEqual("Galaxy", length.ParameterTypes.Single());
    AssertEqual(false, library.TryGetFunction("EmitLn", out _));
    AssertThrows<ArgumentException>(() => new StarStandardLibraryContract([new StarStandardType("Int")], [new StarStandardFunction("Broken", "String")]));

    var compilation = StarCompilation.Create(SourceText.From("Galaxy planets = [];"));
    AssertEqual(false, compilation.HasErrors);
}

static void BackendContractsIsolateEmission()
{
    var descriptor = new StarBackendDescriptor("star.dotnet", "Managed .NET", StarBackendCapabilities.ExecutableArtifact | StarBackendCapabilities.DebugInformation);
    var artifact = new StarBackendArtifact("Mission.dll", "application/vnd.star.dotnet-assembly", [0x53, 0x54, 0x41, 0x52]);
    var emission = new StarBackendEmission([artifact], ImmutableArray<Diagnostic>.Empty);

    AssertEqual("star.dotnet", descriptor.Id);
    AssertEqual(true, descriptor.Capabilities.HasFlag(StarBackendCapabilities.ExecutableArtifact));
    AssertEqual(4, emission.Artifacts.Single().Content.Length);
    AssertEqual(false, emission.HasErrors);
    AssertThrows<ArgumentException>(() => new StarBackendDescriptor("", "Invalid", StarBackendCapabilities.None));
}

static void DotNetSourceBackendEmitsSupportedIr()
{
    var supported = StarIrLowerer.Lower(StarCompilation.Create(SourceText.From("StarFunction Main() { Int fuel = 0; Orbit (Int index = 0; index < 1; index = index + 1) { fuel = fuel + 1; } While (fuel < 2) { fuel = fuel + 1; } SpinWhile { EmitLn(\"launch\"); } (false); Emit(\"!\"); }")));
    var backend = new DotNetSourceBackend();
    var emission = backend.Emit(supported.Ir!);

    AssertEqual(false, emission.HasErrors);
    AssertEqual("star.dotnet.source", backend.Descriptor.Id);
    AssertEqual("Program.cs", emission.Artifacts.Single().Name);
    var source = Encoding.UTF8.GetString(emission.Artifacts.Single().Content.AsSpan());
    AssertEqual(true, source.Contains("Console.WriteLine(\"launch\");", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("Console.Write(\"!\");", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("for (int index = 0; (index < 1); index = (index + 1))", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("while ((fuel < 2))", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("do", StringComparison.Ordinal));

    var unsupported = StarIrLowerer.Lower(StarCompilation.Create(SourceText.From("Constellation Engine { } StarFunction Main() { }")));
    var failedEmission = backend.Emit(unsupported.Ir!);
    AssertEqual(true, failedEmission.HasErrors);
    AssertEqual(0, failedEmission.Artifacts.Length);
    AssertEqual("STR6001", failedEmission.Diagnostics.Single().Code);
}

static void DotNetSourceBackendEmitsExplore()
{
    var compilation = StarCompilation.Create(SourceText.From("StarFunction Main() { Galaxy planets = [1, 2]; Explore (planet in planets) { EmitLn(planet); } EmitLn(planets[0]); }"));
    var lowering = StarIrLowerer.Lower(compilation);
    var backend = new DotNetSourceBackend();
    var emission = backend.Emit(lowering.Ir!);

    AssertEqual(false, emission.HasErrors);
    var source = Encoding.UTF8.GetString(emission.Artifacts.Single().Content.AsSpan());
    AssertEqual(true, source.Contains("object[] planets = new object[] { 1, 2 };", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("foreach (var planet in planets)", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("Console.WriteLine(planets[0]);", StringComparison.Ordinal));
}

static void DotNetSourceBackendEmitsObjectsAndLibrary()
{
    var compilation = StarCompilation.Create(SourceText.From("Constellation Counter { Private Int value = 2; Public Static Int Base = 5; Public StarFunction Next() -> Int { this.value = this.value + 1; return this.value; } } StarFunction Main() { Counter counter = new Counter(); EmitLn(counter.Next()); Galaxy planets = [1, 2]; EmitLn(Length(planets)); }"));
    var lowering = StarIrLowerer.Lower(compilation);
    var emission = new DotNetSourceBackend().Emit(lowering.Ir!);

    AssertEqual(false, emission.HasErrors);
    var source = Encoding.UTF8.GetString(emission.Artifacts.Single().Content.AsSpan());
    AssertEqual(true, source.Contains("public sealed class Counter", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("private int value = 2;", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("public static int Base = 5;", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("public int Next()", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("new Counter()", StringComparison.Ordinal));
    AssertEqual(true, source.Contains("StarRuntime.Length(planets)", StringComparison.Ordinal));
}

static void ConstellationConstructorsCompile()
{
    var compilation = StarCompilation.Create(SourceText.From("Constellation Rocket { Public String Name; Public StarFunction Constructor(String name) { this.Name = name; } } StarFunction Main() { Rocket rocket = new Rocket(\"Nova\"); EmitLn(rocket.Name); }"));
    var lowering = StarIrLowerer.Lower(compilation);
    var emission = new DotNetSourceBackend().Emit(lowering.Ir!);

    AssertEqual(false, compilation.HasErrors);
    AssertEqual(false, emission.HasErrors);
    var source = Encoding.UTF8.GetString(emission.Artifacts.Single().Content.AsSpan());
    AssertEqual(true, source.Contains("public Rocket(string name)", StringComparison.Ordinal));

    var invalid = StarCompilation.Create(SourceText.From("Constellation Rocket { Public StarFunction Constructor(Int fuel) { } } StarFunction Main() { Rocket rocket = new Rocket(\"bad\"); }"));
    AssertEqual(true, invalid.HasErrors);
    AssertEqual("STR4003", invalid.Diagnostics.Single().Code);
}

static void CliRunReturnsStableResults()
{
    var directory = Path.Combine(Path.GetTempPath(), $"star-cli-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var validPath = Path.Combine(directory, "valid.st");
    var invalidPath = Path.Combine(directory, "invalid.st");
    File.WriteAllText(validPath, "StarFunction Main() { EmitLn(\"launch\"); }");
    File.WriteAllText(invalidPath, "Int fuel = @;");

    try
    {
        var valid = RunCli("run", validPath);
        AssertEqual(0, valid.ExitCode);
        AssertEqual($"launch{Environment.NewLine}", valid.StandardOutput);

        var invalid = RunCli("run", invalidPath);
        AssertEqual(1, invalid.ExitCode);
        AssertEqual(true, invalid.StandardError.Contains("invalid.st(1,12): error STR1001", StringComparison.Ordinal));

        var missing = RunCli("run", Path.Combine(directory, "missing.st"));
        AssertEqual(2, missing.ExitCode);
        AssertEqual(true, missing.StandardOutput.Contains("Archivo no encontrado", StringComparison.Ordinal));

        var unknown = RunCli("unknown-command");
        AssertEqual(2, unknown.ExitCode);
        AssertEqual(true, unknown.StandardOutput.Contains("Comando desconocido: unknown-command", StringComparison.Ordinal));
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static (int ExitCode, string StandardOutput, string StandardError) RunCli(params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    startInfo.ArgumentList.Add(typeof(Interpreter).Assembly.Location);
    foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start the Star CLI.");
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return (process.ExitCode, output, error);
}

static void IrLoweringPreservesContracts()
{
    var source = SourceText.From("StarFunction Launch(Int fuel) -> Int { Int thrust = fuel + 1; When (thrust > 0) { EmitLn(thrust); } Otherwise { EmitLn(0); } return thrust; }");
    var compilation = StarCompilation.Create(source);
    var lowering = StarIrLowerer.Lower(compilation);

    AssertEqual(false, compilation.HasErrors);
    var function = AssertType<StarIrFunction>(lowering.Ir!.Statements.Single());
    AssertEqual("Launch", function.Name);
    AssertEqual("Int", function.Parameters.Single().TypeName);
    AssertEqual(SymbolKind.Parameter, function.Parameters.Single().Symbol!.Kind);
    var variable = AssertType<StarIrVariable>(function.Body[0]);
    AssertEqual(SymbolKind.Variable, variable.Symbol!.Kind);
    var addition = AssertType<StarIrBinary>(variable.Initializer!);
    AssertEqual("Int", addition.TypeName);
    AssertEqual(source.Text.IndexOf("fuel + 1", StringComparison.Ordinal), addition.Span.Start);
    AssertType<StarIrConditional>(function.Body[1]);
    AssertType<StarIrReturn>(function.Body[2]);
}

static void IrLoweringRejectsInvalidCompilation()
{
    var compilation = StarCompilation.Create(SourceText.From("StarFunction Main() -> Int { return \"no\"; }"));
    var lowering = StarIrLowerer.Lower(compilation);

    AssertEqual(true, compilation.HasErrors);
    AssertEqual<StarIrCompilationUnit?>(null, lowering.Ir);
    AssertEqual("STR4003", lowering.Diagnostics.Single().Code);
}

static void IrAgreesWithLegacyExecution()
{
    const string program = "StarFunction Boost(Int fuel) -> Int { return fuel + 1; } StarFunction Main() { Int fuel = Boost(2); When (fuel > 2) { EmitLn(\"launch\"); } Otherwise { EmitLn(\"hold\"); } }";
    var compilation = StarCompilation.Create(SourceText.From(program));
    var lowering = StarIrLowerer.Lower(compilation);

    AssertEqual(false, lowering.HasErrors);
    var boost = AssertType<StarIrFunction>(lowering.Ir!.Statements[0]);
    var main = AssertType<StarIrFunction>(lowering.Ir.Statements[1]);
    AssertEqual("Boost", boost.Name);
    AssertType<StarIrReturn>(boost.Body.Single());
    AssertType<StarIrVariable>(main.Body[0]);
    AssertType<StarIrConditional>(main.Body[1]);
    AssertEqual($"launch{Environment.NewLine}", ExecuteLegacy(program));
}

static void IrAgreesWithLegacyAssignmentAndOrbit()
{
    const string program = "StarFunction Main() { Int fuel = 0; Orbit (Int index = 0; index < 2; index = index + 1) { fuel = fuel + 1; } EmitLn(fuel); }";
    var lowering = StarIrLowerer.Lower(StarCompilation.Create(SourceText.From(program)));

    AssertEqual(false, lowering.HasErrors);
    var main = AssertType<StarIrFunction>(lowering.Ir!.Statements.Single());
    var orbit = AssertType<StarIrOrbit>(main.Body[1]);
    AssertType<StarIrVariable>(orbit.Initializer!);
    AssertType<StarIrBinary>(orbit.Condition!);
    AssertType<StarIrAssignment>(orbit.Step!);
    AssertType<StarIrAssignment>(orbit.Body.Single());
    AssertEqual($"2{Environment.NewLine}", ExecuteLegacy(program));
}

static void LegacyNovaCallsReturnUnit()
{
    const string program = "StarFunction Notify() { EmitLn(\"notice\"); }";
    var interpreter = new Interpreter();
    interpreter.Execute(new ASTParser(new Lexer(program).Tokenize()).Parse());
    var call = new CallExpression(new VariableExpression("Notify"), []);

    var result = call.Evaluate(interpreter);
    AssertEqual(StarUnit.Value, result);
}

static void SyntaxParserReportsUnterminatedBlocks()
{
    var result = StarParser.Parse(SourceText.From("StarFunction Main() { EmitLn(\"launch\");"));
    AssertEqual(true, result.HasErrors);
    AssertEqual("STR2002", result.Diagnostics.Single().Code);
}

static void LegacyLexerRecognizesSyntax()
{
    var tokens = new Lexer("When (true) { EmitLn(\"launch\"); }").Tokenize();
    AssertEqual(TokenType.Keyword, tokens[0].Type);
    AssertEqual("When", tokens[0].Value);
    AssertEqual(TokenType.EOF, tokens[^1].Type);
}

static void LegacyParserAcceptsExecutableProgram()
{
    const string program = "StarFunction Main() { Int fuel = 1; When (fuel > 0) { EmitLn(\"launch\"); } }";
    var statements = new ASTParser(new Lexer(program).Tokenize()).Parse();
    AssertEqual(1, statements.Count);
}

static void LegacyRuntimeExecutesBasicProgram()
{
    const string program = "StarFunction Main() { Int fuel = 1; When (fuel > 0) { EmitLn(\"launch\"); } }";
    AssertEqual($"launch{Environment.NewLine}", ExecuteLegacy(program));
}

static string ExecuteLegacy(string program)
{
    var originalOutput = Console.Out;
    using var output = new StringWriter();
    Console.SetOut(output);

    try
    {
        var statements = new ASTParser(new Lexer(program).Tokenize()).Parse();
        new Interpreter().ExecuteWithMainSupport(statements);
    }
    finally
    {
        Console.SetOut(originalOutput);
    }

    return output.ToString();
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected '{typeof(TException).Name}' to be thrown.");
}

static T AssertType<T>(object value) where T : class
{
    if (value is not T typed)
    {
        throw new InvalidOperationException($"Expected '{typeof(T).Name}', received '{value.GetType().Name}'.");
    }

    return typed;
}
