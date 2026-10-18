# Guía de trabajo y continuidad de OpenDBDiff

## Objetivo y lectura inicial

OpenDBDiff compara esquemas Microsoft SQL Server y genera T-SQL de sincronización. Tiene una GUI WinForms y una CLI; la comparación de datos de una tabla es auxiliar.

Al iniciar una sesión:

1. Leer este archivo, [Documentacion/README.md](Documentacion/README.md) y [Documentacion/Seguimiento.md](Documentacion/Seguimiento.md).
2. Ejecutar `git status --short`, comprobar la rama y revisar los cambios relacionados con la tarea. No asumir un árbol limpio.
3. Consultar Codebase Memory como se indica más abajo y contrastar los archivos relevantes con el disco cuando la cobertura/frescura lo requiera.
4. Leer [Documentacion/Ejecucion.md](Documentacion/Ejecucion.md) antes de compilar o ejecutar. Consultar [Arquitectura](Documentacion/Arquitectura.md) y [Documento técnico](Documentacion/DocumentoTecnico.md) según el alcance.
5. Ejecutar la tarea solicitada preservando cambios ajenos y registrar el resultado verificable al finalizar.

## Stack y mapa

- Windows; C#; .NET Framework **4.5.2**; proyectos `.csproj` clásicos y `packages.config`.
- ADO.NET `System.Data.SqlClient`; consultas SQL embebidas; sin dependencia de SMO.
- GUI: `OpenDBDiff`, entrada `OpenDBDiff/Program.cs`, ventana `UI/MainForm.cs`.
- CLI: `OpenDBDiff.CLI`, entrada `Program.cs`, argumentos en `CommandlineOptions.cs`.
- Contratos: `OpenDBDiff.Abstractions.Schema` y `OpenDBDiff.Abstractions.Ui`.
- Motor: `OpenDBDiff.SqlServer.Schema`; lectura en `Generates`, comparación en `Compare`, modelo y emisión SQL en `Model`, recursos en `SQLQueries`.
- Adaptador GUI: `OpenDBDiff.SqlServer.Ui`.
- Pruebas: `OpenDBDiff.Tests`, MSTest clásico.
- Comparador externo local: `OpenDBDiff/ExternalDiff`, `Settings/ExternalDiffSettings.cs` y `UI/ExternalDiffSettingsForm.*`.
- CI existente: `appveyor.yml`, build Release con Visual Studio 2019; licencia GPL v2.

## Compilar, ejecutar y verificar

En Developer PowerShell y desde la raíz, con referencias net452, NuGet y MSTest clásico disponibles:

```powershell
nuget restore .\OpenDBDiff.sln
msbuild .\OpenDBDiff.sln /m /p:Configuration=Debug '/p:Platform=Any CPU'
& .\OpenDBDiff\bin\Debug\OpenDBDiff.exe
vstest.console.exe .\OpenDBDiff.Tests\bin\Debug\OpenDBDiff.Tests.dll
```

Ejemplo de CLI (bases de laboratorio, autenticación integrada):

```powershell
& .\OpenDBDiff.CLI\bin\Debug\OpenDBDiff.CLI.exe -b 'Server=localhost;Database=BaseActual;Integrated Security=True;' -a 'Server=localhost;Database=BaseDeseada;Integrated Security=True;' -o '.\sincronizacion.sql'
```

La CLI transforma `before` → `after`, genera el archivo y devuelve 0/1. Sin `-o`, mezcla el script y mensajes en consola. Para Release, sustituir la configuración y la carpeta de salida. Consultar la guía de ejecución para localizar herramientas y diagnosticar MSB3644; no cambiar el framework ni migrar a SDK-style para superar un bloqueo del entorno.

## Semántica que se debe preservar

- En la GUI, el script transforma **la base derecha hacia la izquierda**: derecha = actual; izquierda = deseada. `MainForm.StartComparison` invierte los pares al construir `ProgressForm`.
- En el comparador externo, los paneles son **izquierda = actual/old**, **derecha = deseado/new**. No confundirlos con los selectores de bases.
- `CompareDatabase.GenerateDifferences` modifica el modelo `origin`; no tratar la comparación como una función pura.
- La CLI invoca `ToSqlDiff` dos veces como workaround documentado en el código. No eliminarlo como limpieza incidental.
- La selección del árbol influye en el script. Las pruebas deben distinguir comparación completa y filtrada cuando se cambie este comportamiento.
- Los scripts pueden incluir DROP/ALTER y reconstrucciones. Generarlos o validarlos no autoriza aplicarlos sobre bases del usuario.
- La configuración se guarda en `%LOCALAPPDATA%\OpenDBDiff\settings.liteDb`; puede contener cadenas de conexión. No versionar ese archivo ni credenciales.
- Abrir la configuración mediante `SettingsDatabase.Open()`, tanto para proyectos como para el comparador externo. Reconoce archivos LiteDB normales y archivos protegidos con DPAPI del usuario actual; no abrir un blob DPAPI directamente con `LiteDatabase` ni reemplazarlo por una base vacía. Los cambios en un archivo protegido conservan el cifrado y generan un respaldo cifrado `.bak` mediante reemplazo atómico.
- `SqlConnectionStringCompatibility.Parse` admite el alias guardado `Trust Server Certificate` como `TrustServerCertificate` sin sustituir texto dentro de contraseñas.
- Los exports del comparador externo viven en `%TEMP%\OpenDBDiff\extdiff` y se limpian entre sesiones de comparación.

## Convenciones y alcance

- Seguir `.editorconfig`: CRLF, salto final, sin espacios al final y cuatro espacios en C#.
- Mantener compatibilidad con el target y las dependencias existentes salvo una migración explícita.
- Registrar nuevos `.cs` y recursos en los `.csproj` clásicos. Mantener coherentes los archivos WinForms `.cs`, `.Designer.cs` y `.resx`.
- No revertir cambios locales ni borrar documentación preexistente. No hacer commit, push o ejecutar scripts SQL por inferencia de esta guía.
- Para cambios de lógica, ejecutar pruebas relevantes y compilar los proyectos afectados. Para cambios de documentación, verificar comandos, rutas y enlaces.
- Distinguir compilación, pruebas unitarias, verificación GUI y ejecución contra SQL Server. Informar los bloqueos reales sin presentar una inspección estática como ejecución.

<!-- codebase-memory-mcp:start -->
## Codebase Memory: reglas del repositorio

Preferir SIEMPRE las herramientas MCP del grafo para descubrir estructura de código. Proyecto observado: `D-GIT-REPOS-OpenDBDiff`, raíz `D:/GIT REPOS/OpenDBDiff`. Confirmar con `list_projects` o `index_status` al iniciar o después de una compactación; en otra ruta puede cambiar el identificador.

Prioridad: `search_graph` para símbolos → `trace_path` para llamadas → `get_code_snippet` para fuente exacta → `check_index_coverage` para evidencia → `query_graph` para relaciones complejas → `get_architecture` para visión global.

- **Scout/Tier 1:** búsqueda positiva provisional; no permite afirmar ausencia o exhaustividad.
- **Verify/Tier 2 (predeterminado):** búsquedas y trazas relevantes, snippets materiales y paginación necesaria.
- **Auditor/Tier 3:** alcance acotado con generación vigente, paginación completa y límites declarados.
- Después de identificar archivos, llamar `check_index_coverage` con todas las rutas de evidencia y con los scopes pertinentes para afirmaciones negativas o exhaustivas.
- Un resultado limpio no demuestra completitud. Si hay cobertura parcial, exclusión, metadatos cambiados, archivos pendientes o cobertura desconocida, leer la fuente y los rangos reportados antes de confiar en el grafo.
- Completar páginas cuando haya `has_more` o `next`. Para literales, errores, configuración y documentos, o resultados insuficientes del MCP, usar `rg`/lectura de archivos.
- Si no hay índice, usar `index_repository` sobre la raíz exacta. Si existe, aprovechar su actualización observada; no crear índices duplicados.
- Antes de delegar, consultar grafo y cobertura en el agente principal; transmitir tier, proyecto, generación/frescura, alcance, consultas, paginación, símbolos, rutas, trazas, rangos pendientes y fallback ya realizado. Un agente sin MCP debe trabajar con esa evidencia y fuente directa sin afirmar acceso al servidor.

Detalles y ejemplos: [Documentacion/CodebaseMemory.md](Documentacion/CodebaseMemory.md).
<!-- codebase-memory-mcp:end -->

## Cierre de sesión

Actualizar [Documentacion/Seguimiento.md](Documentacion/Seguimiento.md) con fecha, alcance, archivos, comandos/resultados, limitaciones y pendientes concretos. Separar lo realizado de propuestas futuras; no inventar prioridades de negocio. Si cambia la forma de ejecutar, actualizar también `Ejecucion.md`; si cambia la arquitectura, actualizar su documento y diagramas.
