# Star 🌟

Star es un lenguaje de programación con sintaxis temática espacial. Este repositorio contiene su compilador: un front-end moderno e independiente, una representación intermedia neutral de plataforma y el intérprete histórico, que se conserva como ruta de compatibilidad.

Las fases 1–14 están verificadas. La Fase 15 está en curso con un emisor .NET de código fuente para un subconjunto explícito del IR, incluidos los bucles `Orbit`, `While` y `SpinWhile`; todavía no produce ensamblados ni ejecuta procesos. Web y JavaScript no forman parte del alcance actual.

## Desarrollo

Requiere el SDK de .NET 10.

```bash
dotnet build Star.sln -c Release --no-restore
dotnet run --project tests/Star.Compiler.Tests/Star.Compiler.Tests.csproj -c Release --no-build
```

La arquitectura, el estado de fases, las decisiones técnicas, el alcance y las reglas de contribución están centralizados en la [documentación canónica](docs/STAR_COMPILER_ROADMAP.md).

## Licencia

El proyecto se distribuye bajo [MIT](LICENSE). Es una licencia permisiva: permite usar, modificar y redistribuir el código conservando el aviso de licencia.
