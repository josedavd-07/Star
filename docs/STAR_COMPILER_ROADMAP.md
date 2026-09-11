# Star — Estado del compilador y hoja de ruta

## Identidad y alcance

El lenguaje se llama **Star**. `StarLang` es solo el nombre del repositorio. La modernización conserva el parser y el intérprete heredados como línea de compatibilidad mientras el front-end independiente madura. No se han introducido targets Web, JavaScript ni generación nativa de programas Star.

## Arquitectura actual

```text
SourceText
  → StarCompilation
      → StarParser
      → StarSemanticAnalyzer
      → StarTypeChecker
      → CompilationResult
  → parser e intérprete heredados (solo para ejecutar programas válidos)
```

`CompilationResult` es el contrato central: conserva texto fuente, árbol inmutable, símbolos, tipos y diagnósticos. Las etapas nuevas viven en `src/Star.Compiler`; el runtime heredado permanece en `StarCompiler/`. La documentación técnica del repositorio se concentra en este archivo; el `README` solo proporciona entrada rápida y enlaces.

## Fases completadas

| Fase | Estado | Resultado |
| --- | --- | --- |
| 1. Base arquitectónica | Completada | `SourceText`, rangos de texto y diagnósticos estructurados. |
| 2. Lexer centralizado | Completada | Tokens con ubicación y diagnósticos `STR1001`–`STR1004`. |
| 3. Árbol estructural | Completada | Árbol inmutable, bloques y diagnósticos de llaves `STR2001`–`STR2002`. |
| 4. Sintaxis explícita | Completada | Declaraciones, expresiones, precedencia, `Orbit`, `SpinWhile` y tipos de `Constellation`. |
| 5. Símbolos y scopes | Completada | Símbolos, scopes léxicos y diagnósticos `STR3001`–`STR3003`. |
| 6. Comprobación de tipos | Completada | Tipos de expresiones y diagnósticos `STR4001`–`STR4003`. |
| 7. Miembros de Constellation | Completada | Campos, métodos, accesibilidad y diagnósticos `STR5001`–`STR5003`. |
| 8. Pipeline centralizado | Completada | `StarCompilation` coordina parser, símbolos, tipos y diagnósticos. |
| 9. Modelo enlazado | Completada | Cada declaración/uso conserva su símbolo; cada expresión, su tipo. |
| 10. Contratos de IR y lowering | Completada | IR inmutable y neutral de plataforma; lowering con resultado explícito para declaraciones, expresiones, llamadas y flujo de control, sin backend. |
| 11. Equivalencia de ejecución y runtime | Completada | Equivalencia IR/intérprete cubierta para funciones, flujo, asignación y `Orbit`; contrato explícito para `Nova`. |
| 12. CLI y pruebas de integración | Completada | Fachada de compilación para la CLI y códigos de salida de proceso reproducibles: éxito (`0`), diagnóstico (`1`) y entrada inválida (`2`). |
| 13. Biblioteca estándar y contratos de runtime | Completada | Contrato inmutable y neutral de plataforma para tipos básicos y firmas de biblioteca, compartido por análisis semántico y de tipos, sin implementación de runtime. |
| 14. Diseño de backends | Completada | Contrato de plugin sobre IR que devuelve artefactos y diagnósticos inmutables, aislado de E/S; primer target seleccionado: ensamblado administrado de .NET, aún sin implementación. |
| 15. Primer backend administrado de .NET | En curso | `DotNetSourceBackend` emite C# reproducible para el subconjunto de IR, incluido el flujo `Orbit`/`While`/`SpinWhile`/`Explore`, colecciones `Galaxy` y objetos `Constellation` básicos; `DotNetAssemblyHost` lo compila explícitamente a un artefacto .NET usado por `star build` y `star run`. |

## Verificación vigente

La última ejecución completa verificada confirmó las fases 1–14:

- Compilación `Release` correcta para `Star.Compiler`, `StarCompiler` y `Star.Compiler.Tests`, sin advertencias ni errores.
- 29 pruebas aprobadas: lexer, parser, expresiones, tipos de `Constellation`, scopes, tipos, miembros, pipeline, contratos de IR/lowering, equivalencia, CLI, contratos de biblioteca estándar, contratos de backend y compatibilidad del intérprete.
- La suite cubre la Fase 15 con emisión C# de variables, asignaciones, retornos, condiciones, llamadas, expresiones binarias, `Emit`/`EmitLn`, colecciones `Galaxy`, acceso por índice y los bucles `Orbit`, `While`, `SpinWhile` y `Explore`; además, comprueba que un nodo fuera del subconjunto devuelve `STR6001` y no produce artefactos. La prueba de proceso reproducible para la CLI cubre `star run` correcto (`0`), fuente inválida (`1`), archivo inexistente (`2`) y comando desconocido (`2`).
- `StarRuntime.Emit` representa de forma segura un valor nulo; las llamadas que no producen un valor Star ahora devuelven el singleton `StarUnit.Value`. El proyecto `StarCompiler` compila con cero advertencias. El cambio es local, preserva el contrato no anulable del AST heredado y está cubierto por la nueva regresión fuente para una función `Nova`.
- `StarStandardLibrary.Core` concentra los tipos centrales (`Int`, `String`, `Bool`, `Galaxy`, entre otros) y firma las APIs futuras `Length`, `Contains` y `ToText`. Es una descripción inmutable de contratos: no ejecuta código, no depende de una plataforma y no altera el intérprete heredado.
- `IStarBackend` recibe `StarIrCompilationUnit` y devuelve `StarBackendEmission`: artefactos y diagnósticos inmutables. La interfaz no descubre plugins, no escribe archivos y no inicia procesos; esas responsabilidades pertenecen al host. Se selecciona ensamblado administrado de .NET como primer target futuro por coherencia con la herramienta actual, sin implementar aún un emisor.
- `DotNetSourceBackend` traduce funciones de nivel superior, variables, asignaciones, retornos, condiciones, llamadas, expresiones binarias, `Emit`/`EmitLn`, listas `Galaxy`, índices y `Orbit`/`While`/`SpinWhile`/`Explore` a C# en memoria. `DotNetAssemblyHost` es el paso separado y explícito que persiste ese C# temporalmente y llama al SDK para producir el `.dll` y sus archivos auxiliares. `star build` deja el artefacto en `bin/Release/<programa>` y `star run` crea un artefacto temporal de depuración y lo ejecuta. Cuando el backend encuentra un nodo, cláusula o tipo fuera del subconjunto, devuelve `STR6001`; el host informa errores del SDK con `STR6002`. Ninguno usa el intérprete heredado como backend.

Ejecutar siempre antes de integrar cambios:

```bash
dotnet build Star.sln -c Release --no-restore
dotnet run --project tests/Star.Compiler.Tests/Star.Compiler.Tests.csproj -c Release --no-build
```

La fase 10 añade el namespace `Star.Compiler.Ir`. `StarIrLowerer.Lower` acepta un `CompilationResult` y devuelve siempre un `StarIrLoweringResult`: conserva los diagnósticos y su propiedad `Ir` es `null` si el front-end contiene errores. Para una compilación válida, `Ir` contiene `StarIrCompilationUnit`. Todos los nodos de IR son inmutables, conservan `TextSpan`, y usan los enlaces de símbolo/tipo existentes cuando están disponibles. El IR no ejecuta programas ni elige plataforma.

## Fases pendientes

### 15. Primer backend administrado de .NET

En curso. `src/Star.Compiler/Backends/DotNetSourceBackend.cs` y `DotNetAssemblyHost` ya generan y compilan artefactos para el subconjunto indicado, incluidos `Orbit`, `While`, `SpinWhile`, `Galaxy`, `Explore` y `Constellation`: campos, propiedades, métodos `Public`/`Private`/`Protected`, miembros `Static`, constructores, instanciación y llamadas a miembros. Una propiedad se declara con `Property`; `Read Property` solo puede asignarse desde su `Constructor` y cualquier otro intento produce `STR5004`. La biblioteca estándar ejecutable incluye `Length`, `Contains` y `ToText`. `star run` ejecuta ese artefacto. El siguiente incremento es tipos de proyecto, manteniendo diagnósticos estructurados y sin usar el intérprete heredado como backend.

### Ruta hacia un Star funcional e independiente

1. **Artefactos administrados:** completado para el subconjunto admitido. El host compila el C# emitido y `star build`/`star run` lo usan sin delegar en el intérprete heredado.
2. **Constellation:** campos, propiedades y métodos con accesibilidad, miembros estáticos, constructores, instanciación y acceso a miembros están completados. Un constructor se declara como `StarFunction Constructor(...)` y debe devolver `Nova`; sus argumentos se validan antes de generar código. Una propiedad se declara como `Public Property String Name;`; `Read Property` genera solo lectura y se inicializa en el constructor.
3. **Biblioteca estándar ejecutable:** `Galaxy`, `Length`, `Contains` y `ToText` están disponibles en el runtime explícito del target. Faltan las futuras APIs que se incorporen al contrato.
4. **Distribución:** `star new console <nombre>` crea un proyecto ejecutable con `.starproj` y `src/Main.st`; `star build` y `star run` son la experiencia actual. `star build --self-contained --runtime <RID>` publica consola o escritorio con su runtime para el identificador de plataforma indicado (por ejemplo, `linux-x64`, `win-x64` o `osx-arm64`). La publicación portable `linux-x64` se verificó ejecutando directamente el binario resultante, sin usar `dotnet`. Falta validar y empaquetar esos artefactos para las demás plataformas soportadas.
5. **Tipos de proyecto:** los targets `console`, `web` y `desktop` están establecidos. `star new web <nombre>` usa `JavaScriptSourceBackend` para transformar el subconjunto admitido del mismo IR en `bin/web/index.html` y `bin/web/app.js`, sin dependencias externas. El runtime web ofrece `Length`, `Contains`, `ToText`, salida visible con `Emit`/`EmitLn`, objetos, condiciones y bucles del subconjunto soportado. `star new desktop <nombre>` usa `AvaloniaDesktopHost` para compilar una ventana nativa de .NET que ejecuta el programa Star y muestra su salida; requiere restaurar la dependencia oficial Avalonia desde NuGet en la primera compilación. Los proyectos no duplican parser, tipos ni semántica.
6. **Independencia de plataforma:** conservar el front-end y el IR propios de Star; .NET será un primer target. Un backend nativo sin .NET es una fase posterior y separada, no una promesa implícita de esta fase.

### Principios de rendimiento

- Star compila antes de ejecutar: la ruta de producto no debe interpretar el árbol sintáctico en cada ejecución.
- El IR conserva tipos, enlaces y flujo de control para que cada backend pueda generar código estático sin reflexión ni búsquedas dinámicas innecesarias.
- El backend .NET produce artefactos Release. El backend nativo futuro podrá optimizar para cada plataforma sin cambiar la sintaxis ni el front-end de Star.
- Las mejoras de rendimiento se medirán con programas representativos y pruebas de regresión; no se cambiará semántica por una optimización sin medición.

## Reglas de repositorio

- `README.md` es la entrada breve; este archivo es la única documentación técnica canónica.
- `.gitignore` excluye resultados de compilación, cobertura, configuraciones locales, secretos de entorno y notas de trabajo. No incluir binarios ni credenciales en cambios.
- `LICENSE` contiene MIT con titular `Star Language Team`. Mantener el aviso en distribuciones del código. La licencia de cualquier activo de terceros debe verificarse antes de publicarlo.

## Criterio para integrar en `main`

Antes de integrar: compilación Release correcta, todas las pruebas verdes, revisión de diagnósticos, revisión de la diferencia contra `main` y confirmación de que no se modificó el intérprete heredado fuera de cambios deliberados y cubiertos por pruebas. Los cambios de esta copia de trabajo deben revisarse y trasladarse de forma explícita al checkout que se vaya a publicar; no se debe sobrescribir el repositorio original sin esa revisión.
