# Seguimiento del proyecto

## Estado al 2026-10-17

- Rama observada: `OpenDiffBuru`.
- HEAD observado: `1787780` (`Add VSCode setting to prefer C# extension`).
- Base técnica: OpenDBDiff 0.13.0, siete proyectos .NET Framework 4.5.2, GUI WinForms y CLI.
- La solicitud de esta sesión fue documentar ejecución y continuidad y dejar Codebase Memory disponible para el proyecto.

## Cambios locales que ya existían

Al comenzar había modificaciones en `OpenDBDiff.csproj`, `OpenDBDiff.Tests.csproj`, `UI/MainForm.cs`, `UI/MainForm.Designer.cs` y `UI/DataCompareForm.cs`. También estaban sin seguimiento Git la carpeta `Documentacion/`, los helpers y pruebas `ExternalDiff`, `Settings/ExternalDiffSettings.cs` y `UI/ExternalDiffSettingsForm.*`.

Se conservaron estos cambios. La implementación local incluye integración con WinMerge/Beyond Compare, exportación por objeto o por conjunto de diferencias y ajustes de comparación de datos. Su presencia no implica que esté publicada o aprobada para una entrega.

## Entregables de documentación de esta sesión

- `AGENTS.md`: reglas, mapa del código, comandos, semántica de sincronización, flujo MCP y cierre de sesión.
- `Documentacion/Ejecucion.md`: preparación, build, GUI, CLI, pruebas y diagnóstico.
- `Documentacion/CodebaseMemory.md`: integración y uso del grafo.
- Este registro y enlaces desde los README existentes.
- Inventario de pruebas del documento técnico actualizado para incluir los helpers externos.

No se cambiaron fuentes C# ni archivos de proyecto como parte de esta solicitud. No se hicieron commits ni push.

## Validación realizada

| Verificación | Resultado |
|---|---|
| Build Debug con MSBuild de VS 2022, sin ajustes | Falló con `MSB3644`: referencias net452 no resueltas |
| Build usando `TargetFrameworkRootPath` local y `ReferencePath` MSTest | Detectó assets residuales que no correspondían al target net452 |
| Build con referencias locales y `ResolveNuGetPackages=false` | Correcto, siete proyectos; advertencia existente CS0618 por `SecurityAction.RequestMinimum` |
| `vstest.console.exe OpenDBDiff.Tests/bin/Debug/OpenDBDiff.Tests.dll /Logger:trx` | **24/24 pruebas correctas** |
| CLI compilada con `--help` | Muestra opciones `before`, `after`, `outputfile`; exit 1 por comportamiento actual de `Main` |
| Codebase Memory MCP | `list_projects`, `index_status`, búsquedas, snippet, traza y comprobación de cobertura respondieron |
| Runtime global Codebase Memory | El wrapper npm 0.12.1 estaba presente, faltaba su binario; se descargó, verificó checksum y `--version` devolvió 0.12.1 |
| Refresco del índice existente | `index_repository` en modo `full` sobre el mismo proyecto: estado `indexed` |
| Documentación | Enlaces locales comprobados en 11 Markdown; los siete bloques PowerShell de `Ejecucion.md` pasan el parser de PowerShell |

Comando de compilación y explicación de los parámetros: [Ejecucion.md](Ejecucion.md). Reporte TRX generado en `TestResults/`, ignorado por Git. Los resultados dependen del entorno y paquetes ya presentes; no se validó una restauración desde cero en otra máquina.

No se lanzó la GUI ni una herramienta externa, no se compararon bases reales ni se aplicaron scripts SQL. El build y las pruebas unitarias no acreditan esas verificaciones.

`git diff --check` sobre el árbol completo reporta whitespace en cambios funcionales preexistentes. No se modificaron esos archivos para corregir formato. La comprobación del diff de `README.md` pasa al reconocer CRLF; los archivos nuevos se revisaron por separado.

## Codebase Memory

Proyecto `D-GIT-REPOS-OpenDBDiff`, raíz `D:/GIT REPOS/OpenDBDiff`. Se reutilizó el servidor MCP ya conectado y se completó el runtime global existente. Las reglas de uso quedaron en `AGENTS.md`. No se ejecutó el instalador general sobre todos los clientes del equipo, porque este cliente ya exponía el servidor.

## Pendientes sugeridos

1. Probar preparación/restauración desde un checkout limpio y decidir cómo proveer de forma reproducible las referencias net452 y MSTest clásico.
2. Verificar manualmente GUI y comparador externo con bases de laboratorio: objeto creado, eliminado, modificado y comparación de todas las diferencias.
3. Validar generación y, si se solicita expresamente, aplicación del script en bases de laboratorio; comprobar el resultado con una segunda comparación.
4. Revisar los cambios funcionales locales antes de preparar una entrega o commit.

Estos puntos son propuestas de seguimiento, no trabajo iniciado ni una prioridad de negocio acordada.

## Registro para próximas sesiones

Agregar una entrada con fecha, solicitud, cambios concretos, comandos ejecutados, resultado y pendientes. Actualizar el estado vigente si cambia la rama, el stack, la ejecución o el índice. No reemplazar evidencia anterior con afirmaciones de éxito sin validación.

## 2026-10-17: conexión MCP del proyecto

Solicitud: corregir la desconexión del grafo en OpenDBDiff. Se preservaron todos los cambios funcionales y documentales anteriores.

Se reprodujo un conflicto de instalaciones: el wrapper npm 0.12.1 no puede iniciar contra la caché del daemon administrado 0.10.6 activo. La conexión MCP que ya tenía esta sesión sí funcionaba; esto no acreditaba que otro lanzador pudiera abrir una conexión nueva. La ruta administrada se confirmó fuera del entorno restringido; su ausencia aparente dentro de ese entorno no era evidencia de un ejecutable eliminado.

Cambios: `.codex/config.toml` configura el servidor del proyecto con 120 segundos de arranque/herramienta; `.codex/start-codebase-memory.cmd` selecciona la instalación administrada mediante LOCALAPPDATA, sin depender de la versión npm de PATH. Se amplió `Documentacion/CodebaseMemory.md` con causa, configuración y límites.

Validación: parser TOML correcto; lanzador `--version` = **0.10.6**; proceso STDIO independiente con `initialize` y `index_status` = **correcto**, proyecto `D-GIT-REPOS-OpenDBDiff`, raíz exacta y estado `ready`. El diagnóstico temporal cerró solamente su propio árbol de procesos. Se consultaron además `list_projects`, `search_graph`, `get_code_snippet`, `check_index_coverage` e `index_status` por MCP; `index_repository` modo `full` refrescó el mismo proyecto. Los conteos varían con el watcher; siguen registrados 14 SQL parciales. No se compilaron fuentes C# ni se ejecutó SQL.

Pendiente de interfaz: reiniciar la conexión MCP del cliente para que cargue la configuración local; no se ha verificado visualmente su indicador de conexión. Codex carga configuración local únicamente en proyectos de confianza. No se modificó la configuración global ni se detuvieron sesiones de otros clientes. No se hicieron commits ni push.

## 2026-10-17: corrección del error de arranque

El usuario reportó **This data file is encrypted and needs a password to open** al ejecutar la GUI compilada. El archivo local era un blob Windows DPAPI que contenía una base LiteDB legible; `Project` y `ExternalDiffSettings` intentaban abrirlo directamente como LiteDB. Se comprobó el descifrado en memoria con el usuario actual sin mostrar conexiones o contraseñas.

Se añadió `OpenDBDiff/Settings/SettingsDatabase.cs` como acceso compartido. Reconoce DPAPI y LiteDB directo, descifra solamente en memoria, conserva el formato protegido al escribir y reemplaza atómicamente el archivo con respaldo cifrado `.bak`. No se migra ni se borra la configuración existente. Ambos consumidores de settings usan este acceso.

La comprobación posterior detectó un segundo fallo al restaurar las conexiones: `System.Data.SqlClient` rechazaba el alias `Trust Server Certificate`. Se añadió `SqlConnectionStringCompatibility.Parse` en `SqlServer.Ui/Front/Util` y se integró en el selector. Normaliza el nombre de la opción usando un parser de cadenas, conservando los valores y las contraseñas que contienen separadores.

Validación:

- Una prueba reprodujo el mensaje exacto del archivo cifrado antes de implementar la corrección; otra reprodujo el rechazo del alias de certificado.
- La solución Debug compiló con los parámetros de referencias locales documentados. Los `.csproj` incluyen los nuevos archivos y las referencias `System.Security`, `System.Data` y LiteDB necesarias para las pruebas.
- **32/32 pruebas correctas**, incluidas seis de configuración y dos del alias de conexión. Se cubre que un fallo de reemplazo no altere el original y limpie el temporal.
- Un probe temporal .NET Framework en `TestResults/` leyó la última configuración y los ajustes externos, construyó el formulario y ejecutó `MainForm_Load` con la configuración real: **correcto**. Verificó que los bytes del archivo original permanecieran idénticos.
- El probe no abrió conexiones SQL ni mostró valores guardados. No se validó el recorrido visual completo ni la comparación contra bases reales.

El binario corregido quedó en `OpenDBDiff/bin/Debug/OpenDBDiff.exe`. No se hicieron commits ni push. La compatibilidad DPAPI requiere el contexto del usuario Windows que protegió el archivo; no permite abrir archivos de otro perfil sin sus claves.

Referencia de la API empleada: [Microsoft: ProtectedData.Unprotect](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata.unprotect?view=netframework-4.8.1).

## 2026-10-17: diagnóstico de 172 tablas en CuriosityPOS vs CuriosityPROD

Solicitud: explicar por qué la comparación muestra 172 tablas cuando se esperan 218. Se conservaron los cambios locales anteriores; no se modificaron fuentes de la aplicación, no se compiló la solución ni se ejecutó SQL de sincronización.

Evidencia reproducida con los binarios Debug existentes y autenticación integrada:

- La última configuración guardada apunta a `(local)/CuriosityPOS` y `(local)/CuriosityPROD`. Se leyeron únicamente para confirmar servidor, base y modo de autenticación; no se imprimieron credenciales ni se escribió la configuración.
- Consulta de catálogo `sys.tables WHERE is_ms_shipped=0`: CuriosityPOS = **219**, CuriosityPROD = **229**. POS incluye una tabla `sysdiagrams`; excluyéndola quedan **218**. POS contiene 12 tablas optimizadas para memoria en el esquema `inMem`.
- El lector `GenerateTables.Fill` carga solo **162** tablas de POS y **166** de PROD. En ambas bases registra `Unable to cast object of type 'System.DBNull' to type 'System.String'` al leer `Transaction`, en `GenerateTables.cs:261`.
- La consulta de metadatos confirmó que `[inMem].[Transaction]` es memory-optimized y sus índices tienen `data_space_id=0`; el LEFT JOIN a `sys.data_spaces` devuelve NULL. Es distinta de `[dbo].[Transaction]`, que usa `MonthlyPS`.
- `FillTables` convierte `reader[FileGroupIndex]` directamente a `string`. El `catch` de `Fill` guarda el error y termina la lectura de todas las tablas pendientes. `Generate.Process` conserva la lista privada de mensajes y devuelve el modelo parcial.
- Reproducción con `Generate.Process` para cada base y `Generate.Compare(PROD, POS)`: **166**, **162**, resultado combinado **172**. Esto reproduce exactamente el conteo reportado, antes de aplicar filtros visuales.

Comandos y artefactos de diagnóstico:

- `sqlcmd -S localhost -E -C` con SELECT de catálogo únicamente. El acceso inicial dentro del entorno restringido falló; las consultas fuera de ese entorno funcionaron.
- Probes .NET Framework compilados con `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, contra las DLL existentes de `OpenDBDiff/bin/Debug`: `TestResults/TableProbe.cs` y `TestResults/CompareProbe.cs`, con sus ejecutables. `TestResults/` está ignorado por Git. El segundo probe comprobó la configuración guardada y reprodujo la comparación sin generar ni aplicar SQL.
- Grafo MCP existente consultado en Tier 2, generación `2026-10-18T03:47:29Z`; cobertura de rutas con `metadata_changed`. Las conclusiones materiales se contrastaron con la fuente del disco y ejecución real del lector.

Conclusión: el conteo 172 procede de una lectura interrumpida, no del número real de tablas ni exclusivamente de los filtros del árbol. Las 218 esperadas corresponden al inventario de POS sin `sysdiagrams`; el árbol de comparación combina objetos de ambas bases, por lo que su total completo puede ser diferente.

Pendiente concreto: corregir la lectura de metadatos de tablas optimizadas para memoria y evitar presentar modelos parciales como una comparación completa. Revisar también el soporte de emisión T-SQL para estos objetos antes de considerar seguro el script generado; sustituir NULL por texto vacío por sí solo no acredita soporte de MEMORY_OPTIMIZED ni de índices HASH. Esta sesión concluye con diagnóstico reproducido, sin corrección funcional ni validación visual de la GUI.


## 2026-10-17: implementación de sección y soporte de tablas en memoria

Se implementó el plan aprobado: lectura y comparación de tablas en memoria, sección propia en Schema, generación SQL específica y bloqueos preventivos. Se conservaron las modificaciones locales previas, .NET Framework 4.5.2 y el sentido derecha a izquierda. No se hicieron commits ni push.

Cambios principales:

- `Table` conserva `IsMemoryOptimized`, `Durability`, instantánea original y dependencias externas; `Index` añade HASH y `BucketCount`. Se conserva el orden de columnas en claves compuestas y se evita duplicar claves como índices independientes.
- Lectores y consultas de tablas/índices/restricciones incorporan los metadatos desde SQL Server 2014, conservan las consultas anteriores para versiones antiguas y admiten el grupo de archivos NULL. Los errores de lectura cancelan la comparación con base, etapa y objeto, en vez de entregar un inventario parcial.
- `MemoryOptimizedTableSql` concentra definición, comparación, capacidades y validación de toda la selección. HASH utiliza ALTER INDEX REBUILD para modificar buckets. Las modificaciones directas se generan desde SQL Server 2016; conversiones, cambios de durabilidad, reordenamientos que exijan reconstrucción y dependencias externas bloquean la generación con motivos agrupados.
- Schema separa `Tables` y `Memory-Optimized Tables`, muestra visibles/total, conserva selección al filtrar y explica los bloqueos. `Database.Tables` sigue siendo una colección única. La identidad de selección combina tipo y nombre completo para evitar colisiones de IDs entre tablas/índices.
- La GUI conserva el árbol cuando se bloquea la generación y limpia/deshabilita el script y su exportación. Guardar valida antes de abrir el archivo; las actualizaciones prevalidan el lote y cada objeto. La CLI conserva su workaround de doble `ToSqlDiff` y no abre el archivo de salida cuando la validación falla.
- Nuevos `MemoryOptimizedTests` y probes reproducibles `OpenDBDiff.Tests/Integration/MemoryLab.cs` y `MemoryGuiProbe.cs`. Se registraron los archivos y la referencia del motor en los proyectos clásicos. No se añadieron paquetes.
- Documentación: `TablasEnMemoria.md`, ejecución, arquitectura, documento técnico, diagramas, índice y registro `tasks/`.

Verificación final:

| Comprobación | Resultado |
|---|---|
| Regresión inicial | Dos pruebas nuevas fallaron antes de añadir los metadatos; la lectura real anterior reprodujo DBNull y 172 tablas |
| Build Debug | Correcto, siete proyectos, usando las referencias locales documentadas; advertencia preexistente CS0618 |
| MSTest completo | **58/58** correctas; TRX `TestResults/sykgu_DESKTOP-6FBORC3_2026-10-17_22_39_20.trx` |
| Lectura real, sin DDL | CuriosityPOS **219**, CuriosityPROD **229**, ambas con **12** tablas en memoria |
| Panel WinForms | **240** nodos de tabla sin duplicados: **228** convencionales y **12** en memoria; filtros y selección comprobados; imagen `TestResults/memory-schema.png` |
| Generación real, sin ejecución | Detecta dependencia de `[inMem].[Transaction]` con `[inMem].[ins_NativeinMem_TransactionVPH]`; al seleccionar las convencionales sí genera su SQL |
| SQL de laboratorio | Creación, ALTER, DEFAULT, claves HASH/compuestas, UNIQUE/CHECK/FK, eliminación, preservación de datos y segunda comparación sin diferencias: correctos |
| CLI de laboratorio | Conversión bloqueada: exit 1 y archivo de salida preexistente intacto |
| Documentación | Diez bloques PowerShell pasan el parser y los enlaces locales comprobados existen |
| Diff del alcance nuevo | `git -c core.whitespace=blank-at-eol,blank-at-eof,cr-at-eol diff --check` en motor, selección, Schema y Updater: correcto |

El laboratorio final utilizó SQL Server **17.0.1145.4**, con las bases nuevas `OpenDBDiffMemoryTest_230fae33f2874118b0b33df5699427b5_Actual` y `_Desired`. Se eliminaron en la limpieza del probe. Los SQL de prueba quedaron en `TestResults/01-create.sql`, `02-alter.sql`, `02b-default-change.sql` y `03-drop.sql`, ignorados por Git. No se ejecutaron scripts de sincronización sobre CuriosityPOS ni CuriosityPROD.

Límites verificables: no se instalaron motores SQL Server 2014/2016 para esta prueba; su selección de capacidades y bloqueos se cubre unitariamente. Los tipos de índice de memoria distintos de HASH/no agrupado se muestran como no soportados y bloquean la generación. No hay migración automática de datos entre disco/memoria, ni resolución automática de dependencias nativas. El probe WinForms instancia y renderiza el control real, sin recorrer manualmente todos los diálogos.

El MCP del grafo devolvió `Transport closed` tanto en `index_status` como en la comprobación final de cobertura de todas las rutas. Se utilizó lectura directa de fuentes, diff, compilación y ejecución; no se afirma cobertura actual del grafo ni se crearon índices duplicados. Hubo una interrupción temporal de las herramientas locales; tras recuperarse se repitieron compilación y pruebas necesarias.

Binario de esa validación: `OpenDBDiff/bin/Debug/OpenDBDiff.exe`. En ese punto, la dependencia nativa todavía bloqueaba el script completo; la revisión posterior se registra a continuación. No se inició ninguna intervención sobre las bases reales.

## 2026-10-17: revisión del bloqueo de Transaction

El mensaje sobre `[inMem].[ins_NativeinMem_TransactionVPH]` se reprodujo y se revisó con lectura de catálogo. La diferencia real de `[inMem].[Transaction]` es el DEFAULT de `TrTimeStamp`: CuriosityPROD tiene `(getdate())` y CuriosityPOS no. Columnas, clave HASH y definición emitible del índice no agrupado coinciden. La sincronización derecha a izquierda debe quitar ese DEFAULT de PROD.

El bloqueo era demasiado amplio por dos motivos. Trataba cualquier dependencia enlazada al esquema como impedimento aunque la operación solo quitara un DEFAULT, y comparaba `IsAutoStatistics`, opción convencional que no forma parte de la sintaxis emitida para un índice en memoria. `MemoryOptimizedTableSql` ahora compara solamente las propiedades representables del índice y permite diferencias exclusivamente de DEFAULT; conserva el bloqueo para cambios estructurales, conversiones, reconstrucciones y demás operaciones protegidas.

Validación: las regresiones fallaron antes del cambio y pasaron después; el probe de Curiosity, de solo lectura, confirmó `ScriptAllowed=True` y `DropsTimestampDefault=True`. El laboratorio en SQL Server 17.0.1145.4 creó un procedimiento nativo enlazado a una tabla desechable, aplicó el cambio de DEFAULT, conservó el procedimiento y obtuvo una segunda comparación sin diferencias. Las bases `OpenDBDiffMemoryTest_91edb5bd3257459abedf1c702033363c_Actual` y `_Desired` se eliminaron al finalizar. No se ejecutó DDL en CuriosityPOS ni CuriosityPROD.

## 2026-10-17: organización de commits y push del proyecto

Solicitud: generar commits y push del proyecto documentando los cambios en unidades lógicas.

Se organizaron los cambios en commits atómicos y verificados de manera incremental:

1. `feat(schema)`: soporte para tablas optimizadas para memoria en el motor y modelo SQL Server (`OpenDBDiff.Abstractions.Schema`, `OpenDBDiff.SqlServer.Schema`).
2. `feat(ui,settings)`: comparador externo (WinMerge/Beyond Compare), persistencia DPAPI transparente en `SettingsDatabase`, compatibilidad con alias de certificado en `SqlServerConnectFront`, sección dedicada de tablas en memoria en `SchemaTreeView`, validación preventiva en `MainForm`/`DataCompareForm`, y suite completa de pruebas unitarias/sondas de integración.
3. `docs`: documentación técnica, arquitectura, diagramas, guías de ejecución, especificación de tablas en memoria y configuración MCP de Codebase Memory.

Validación previa y posterior a la creación de commits:
- Build Debug con MSBuild (7 proyectos): **0 advertencias, 0 errores**.
- MSTest: **60/60 pruebas exitosas**.
- Publicación mediante `git push origin OpenDiffBuru`.

