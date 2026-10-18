# Documento técnico de OpenDBDiff

Versión documentada: **0.13.0** · Framework: **.NET Framework 4.5.2** · Licencia: **GNU GPL v2**

Este documento describe tecnologías, frameworks, modelos, algoritmos y puntos de extensión. Complementa [Arquitectura.md](Arquitectura.md).

## 1. Stack tecnológico

### 1.1 Plataforma

| Elemento | Valor |
|---|---|
| Lenguaje | C# |
| Runtime | .NET Framework 4.5.2 |
| IDE histórico | Visual Studio 2015+ (solución formato 12.00; CI en VS 2019) |
| UI | Windows Forms |
| Acceso a datos | ADO.NET `System.Data.SqlClient` |
| Empaquetado | packages.config (NuGet clásico, no SDK-style) |
| CI | AppVeyor |
| Control de versiones | Git / GitHub |

No se usa Entity Framework, Dapper, SMO (`Microsoft.SqlServer.Smo`) ni .NET Core / .NET 5+.

### 1.2 Paquetes NuGet

| Paquete | Versión | Proyecto | Uso |
|---|---|---|---|
| DiffPlex | 1.6.3 | GUI | Diff lado a lado del T-SQL de un objeto |
| jacobslusser.ScintillaNET | 3.6.3 | GUI | Editor del script de sincronización (lexer `mssql`) |
| LiteDB | 5.0.9 | GUI | Proyectos y última configuración |
| Essy.Tools.InputBox | 1.0.0 | GUI | Diálogos de texto |
| CommandLineParser | 2.8.0 | CLI | `-b/--before`, `-a/--after`, `-o/--outputfile` |
| TSQL.Parser | 1.5.2 | SqlServer.Schema | Normalización y comparación de cuerpos T-SQL |
| Moq | 4.7.145 | Tests | Mocks |
| Castle.Core | 4.4.1 | Tests | Dependencia de Moq |
| System.ValueTuple | 4.5.0 | Tests | Compatibilidad net452 |
| System.Threading.Tasks.Extensions | 4.5.4 | Tests | Compatibilidad |
| System.Runtime.CompilerServices.Unsafe | 4.7.1 | Tests | Compatibilidad |

### 1.3 Bibliotecas de la plataforma

- `System.Windows.Forms` — GUI
- `System.Data.SqlClient` — conexiones y lectores
- `System.Reflection` — árbol de esquema vía `[SchemaNode]`
- MSTest — `Microsoft.VisualStudio.TestTools.UnitTesting`

### 1.4 Versiones de SQL Server reconocidas

`DatabaseInfo.SQLServerVersion` en [`OpenDBDiff.SqlServer.Schema/Model/DatabaseInfo.cs`](../OpenDBDiff.SqlServer.Schema/Model/DatabaseInfo.cs):

SQL Server 2000, 2005, 2008, 2008 R2, Azure (`SQLServerAzure10`), 2012, 2014, 2016, 2017.

Ediciones: Personal, Standard, Enterprise, Express, Azure.

El README declara soporte desde **2005+**. 2000 aparece en el enum por compatibilidad histórica.

## 2. Puntos de entrada

### 2.1 GUI

[`OpenDBDiff/Program.cs`](../OpenDBDiff/Program.cs): `[STAThread] Main()` → `Application.Run(new MainForm())`.

Formularios relevantes (`OpenDBDiff/UI/`):

| Clase | Función |
|---|---|
| `MainForm` | Ventana principal |
| `ProgressForm` | Carga + comparación (síncrono en `Activated`) |
| `SchemaTreeView` | Árbol con checkboxes y color por `ObjectStatus` |
| `OptionForm` | Contenedor de opciones |
| `ListProjectsForm` | Proyectos guardados |
| `DataCompareForm` | Comparación de datos de una tabla |
| `ErrorForm` | Errores |

### 2.2 CLI

[`OpenDBDiff.CLI/Program.cs`](../OpenDBDiff.CLI/Program.cs) + [`CommandlineOptions.cs`](../OpenDBDiff.CLI/CommandlineOptions.cs).

```text
OpenDBDiff.CLI.exe -b "<cs before>" -a "<cs after>" [-o sync.sql]
```

- Exit code `0` éxito, `1` error.
- Opciones de comparación: siempre `new SqlOption()` (no hay flags de filtro).
- `ToSqlDiff` se invoca dos veces a propósito (workaround alineado con la GUI).

## 3. Modelo de esquema

### 3.1 Jerarquía

```
ISchemaBase
  └── SchemaBase
        └── SQLServerSchemaBase
              ├── Database : IDatabase
              ├── Table, Column, Constraint, Index, …
              └── Code
                    ├── StoredProcedure
                    ├── View
                    ├── Function
                    └── Trigger (según el tipo)
```

`SQLServerSchemaBase` aporta delimitadores `[` `]` y extended properties.

### 3.2 Database

[`OpenDBDiff.SqlServer.Schema/Model/Database.cs`](../OpenDBDiff.SqlServer.Schema/Model/Database.cs) es la raíz. Mantiene `SchemaList<T,P>` para cada familia de objetos, un índice `AllObjects` (`SearchSchemaBase`) y un grafo `Dependencies`.

Métodos clave:

- `BuildDependency()` — tras la carga.
- `Find(fullName)` / `Find(id)` — resolución de objetos anidados.
- `ToSqlDiff(schemas)` — orquesta el script; si `schemas` no está vacío, solo incluye lo marcado en el árbol.

### 3.3 ObjectStatus

Flags en [`StatusEnum.cs`](../OpenDBDiff.Abstractions.Schema/StatusEnum.cs):

| Valor | Significado |
|---|---|
| `Original` | Sin cambios |
| `Create` | Hay que crearlo |
| `Drop` | Hay que eliminarlo |
| `Alter` | Cambio estructural o de definición |
| `AlterBody` | Cambio de cuerpo (objetos de código) |
| `AlterWhitespace` | Solo whitespace (según opciones) |
| `Rebuild` | DROP + CREATE (p. ej. tabla) |
| `RebuildDependencies` | Recrear dependientes |
| `Disabled` | Change tracking / deshabilitado |
| `ChangeOwner` | Cambio de propietario |
| `PermissionSet` | Cambio de permission set CLR |
| `Bind` | Bind de reglas/tipos |

### 3.4 ObjectType

Incluye Table, Column, Trigger, Constraint, Index, UserDataType, XMLSchema, View, Function, StoredProcedure, Schema, FileGroup, Assembly, User, Role, Synonym, FullText, PartitionFunction, PartitionScheme, TableType, CLR*, ExtendedProperty, Rule, Default, entre otros.

## 4. Lectura del catálogo

Orquestador: [`Generate.Process()`](../OpenDBDiff.SqlServer.Schema/Generates/Generate.cs).

Orden aproximado:

1. `GenerateDatabase.Get` — versión, collation, edición.
2. Rules, Tables, Views.
3. Index / FullTextIndex si `Options.Ignore.FilterIndex`.
4. UserDataTypes, XMLSchemas, Schemas.
5. Si no es Azure: PartitionFunctions, PartitionSchemes, FileGroups.
6. DDLTriggers, Synonyms.
7. Si no es Azure: Assemblies, FullText.
8. StoredProcedures, Functions, Triggers, TextObjects, Users.
9. ExtendedProperties.
10. `BuildDependency()`.

Las consultas viven como recursos embebidos en [`SQLQueries/`](../OpenDBDiff.SqlServer.Schema/SQLQueries/) (`SQLQueryFactory`) y como builders en `Generates/SQLCommands/` (p. ej. `TableSQLCommand`).

Progreso: `ProgressEventArgs` → `IGenerator.OnProgress` → barras de `ProgressForm`.

Errores parciales (p. ej. en tablas) se acumulan en `MessageLog` y no abortan necesariamente toda la carga. Fallos de versión pueden lanzar `SchemaException`.

## 5. Comparación

### 5.1 Orquestación

[`CompareDatabase.GenerateDifferences`](../OpenDBDiff.SqlServer.Schema/Compare/CompareDatabase.cs) compara, en este orden:

Tables → Assemblies → UserTypes → XmlSchemas → Schemas → FileGroups → Rules → DDLTriggers → Synonyms → Users → Procedures → CLRProcedures → CLRFunctions → Views → Functions → Roles → PartitionFunctions → PartitionSchemes → TableTypes → FullText.

Devuelve el **origin mutado** y asigna `SourceInfo = destination.Info`.

### 5.2 Algoritmo base

[`CompareBase<T>.GenerateDifferences`](../OpenDBDiff.SqlServer.Schema/Compare/CompareBase.cs):

- En destination y no en origin → `DoNew` → `Create`.
- En ambos → `DoUpdate` (especializado).
- En origin y no en destination → `DoDelete` → `Drop`.

### 5.3 Ejemplo: tablas

[`CompareTables`](../OpenDBDiff.SqlServer.Schema/Compare/CompareTables.cs) delega en:

- `CompareColumns`
- `CompareConstraints`
- `CompareIndexes`
- `CompareTablesOptions`
- `CompareTriggers` / `CompareCLRTriggers`
- `CompareFullTextIndex`

Además compara filegroup y change tracking (`Rebuild` / `Disabled`).

### 5.4 Objetos de código

SPs, views y functions comparan el texto (normalizado con TSQL.Parser / `FormatCode`). Si solo cambia whitespace y la opción lo permite → `AlterWhitespace`; si no → `Alter`.

## 6. Generación de script

### 6.1 Orden macro (`Database.ToSqlDiff`)

Cabecera (versión, usuario, máquina, source/destination) → `USE [db]` (no Azure) → Assemblies → UserTypes → TableTypes → Tables → Rules → Schemas → XmlSchemas → Procedures → CLR* → FileGroups → DDLTriggers → Synonyms → Views → Users → Functions → Roles → Partitions → FullText.

### 6.2 Orden fino (`SQLScript.CompareTo`)

1. `Deep` (profundidad de dependencias).
2. `ScriptAction` (el enum está numerado para que DROP vaya antes que ADD en el sentido correcto).
3. Peso `Dependencies` (sentido invertido en algunos DROP de tabla/constraint/trigger).

### 6.3 Patrones por status

| Status | Mecanismo típico |
|---|---|
| Create | `ToSqlAdd()` / `Create()` |
| Drop | `ToSqlDrop()` → `DROP … GO` |
| Alter | `ToSQLAlter()` (`ALTER TABLE`, `ALTER PROC`, …) |
| Rebuild (tabla) | Tabla temporal + `sp_rename` + copia de datos |
| RebuildDependencies | DROP dependientes → cambio → CREATE dependientes |

Si un objeto marcado no produce SQL, `SQLScriptList.WarnMissingScript` inserta un comentario `-- DIFF-ERROR … Missing script`.

### 6.4 Aplicación directa

[`Updater`](../OpenDBDiff/Updater.cs) ejecuta `ToSql()` / alter contra la connection string **derecha** (`SqlCommand.ExecuteNonQuery`). No es un migrador transaccional de todo el script.

## 7. Interfaz de usuario

### 7.1 Arranque

`MainForm_Load` registra `SQLServerProjectHandler`, crea selectores izquierda/derecha y restaura `Project.GetLastConfiguration()`.

### 7.2 Compare

`StartComparison`:

1. Generadores source (izquierda) y destination (derecha).
2. `ProgressForm(rightPair, leftPair, comparer)` — **right = origin, left = destination**.
3. Script en Scintilla desde `progress.Destination.ToSqlDiff`.
4. Árbol: LeftDatabase = destination (con diff), RightDatabase = origin clonado.
5. DiffPlex al seleccionar un nodo (`ToSql()` old vs new).
6. Guarda última configuración.

Colores del árbol (convención habitual del control): Create verde, Drop rojo, Alter azul, Original sin resaltar.

### 7.3 Opciones (`SqlOption`)

| Bloque | Clase | Qué controla |
|---|---|---|
| Ignore | `SqlOptionIgnore` | Qué tipos y subpropiedades se leen/comparan (~40 flags) |
| Filters | `SqlOptionFilter` | Exclusiones por nombre/patrón (`sys`, `dbo.dtproperties`, …) |
| Comparison | `SqlOptionComparison` | Case sensitivity, whitespace en código |
| Defaults | `SqlOptionDefault` | DEFAULT al añadir columnas |
| Script | `SqlOptionScript` | Schema binding y comportamiento de script |

UI: `SqlOptionsFront` + `AddExclusionPatternForm`.

### 7.4 Proyectos

[`OpenDBDiff/Settings/Project.cs`](../OpenDBDiff/Settings/Project.cs):

- Archivo: `%LocalAppData%\OpenDBDiff\settings.liteDb`
- Colección `projects`
- Campos: connection strings, nombre, tipo `SQLServer`, `IsLastConfiguration`, opciones

## 8. Errores y diagnóstico

| Capa | Comportamiento |
|---|---|
| `CompareDatabase` | `SchemaException` con inner exception |
| `GenerateTables.Fill` | `MessageLog` (continúa) |
| `ProgressForm` | Captura, asigna `Error`, cierra |
| `MainForm` | `ErrorForm` |
| `SchemaException` | Log en `%TEMP%\OpenDBDiff.log` |
| CLI | Mensaje en consola, exit 1 |
| `App.config` | `OpenDBDiff.NewIssueUri` para reportar issues |

`ProgressForm` tiene un TODO de reporte de errores thread-safe; hoy no hace falta porque todo corre en el hilo UI.

## 9. Pruebas

Proyecto: [`OpenDBDiff.Tests`](../OpenDBDiff.Tests/).

| Test | Qué cubre |
|---|---|
| `FormatCodeTests` | Normalización de CREATE de triggers (snippets embebidos) |
| `ColumnsTests` | `CompareColumns`, DROP de columna extra, filtro de `ToSqlDiff` |
| `ByteToHexEncoderTests` | Encoding de assemblies CLR |
| `ExternalDiffHelperTests` | Nombres/rutas de exportación, placeholders y presets WinMerge/Beyond Compare |
| `SettingsDatabaseTests` | Lectura/guardado de configuración DPAPI, preservación del cifrado y respaldo, compatibilidad LiteDB y fallos sin sobrescritura |
| `SqlConnectionStringCompatibilityTests` | Alias de certificado con espacios, preservación de valores y contraseñas con separadores |

No hay tests de integración contra SQL Server, ni de WinForms, ni del CLI end-to-end.

Los helpers de comparación externa pertenecen a cambios locales presentes en esta rama; se compilan mediante archivos enlazados desde el proyecto de pruebas. Para comandos y resultados observados, consultar [Ejecucion.md](Ejecucion.md) y [Seguimiento.md](Seguimiento.md).

## 10. Build y CI

[`appveyor.yml`](../appveyor.yml):

- Versión `0.13.0.{build}`
- Parchea `OpenDBDiff/Properties/AssemblyVersionInfo.cs`
- `nuget restore OpenDBDiff.sln`
- Build Release paralelo
- Artefactos: `OpenDBDiff\bin\Release` y `OpenDBDiff.CLI\bin\Release`

## 11. Inventario de archivos del motor

### Comparadores (`SqlServer.Schema/Compare/`)

CompareDatabase, CompareBase, CompareTables, CompareColumns, CompareColumnsConstraints, CompareConstraints, CompareIndexes, CompareTablesOptions, CompareTriggers, CompareCLRTriggers, CompareFullTextIndex, CompareViews, CompareStoredProcedures, CompareFunctions, CompareAssemblies, CompareUserDataTypes, CompareXMLSchemas, CompareSchemas, CompareFileGroups, CompareRules, CompareDDLTriggers, CompareSynonyms, CompareUsers, CompareRoles, CompareCLRStoredProcedure, CompareCLRFunction, ComparePartitionFunction, ComparePartitionSchemes, CompareTableType, CompareFullText.

### Generadores (`SqlServer.Schema/Generates/`)

Generate, GenerateDatabase, GenerateTables, GenerateViews, GenerateIndex, GenerateFullTextIndex, GenerateUserDataTypes, GenerateXMLSchemas, GenerateSchemas, GeneratePartitionFunctions, GeneratePartitionScheme, GenerateFileGroups, GenerateDDLTriggers, GenerateSynonyms, GenerateAssemblies, GenerateFullText, GenerateStoredProcedures, GenerateFunctions, GenerateTriggers, GenerateTextObjects, GenerateUsers, GenerateExtendedProperties, GenerateRules, GenerateDefaults, GenerateConstraint.

## 12. Cómo extender

Añadir un tipo de objeto SQL Server:

1. Modelo en `SqlServer.Schema/Model` (heredar `SQLServerSchemaBase` o `Code`).
2. Query en `SQLQueries` o `SQLCommands`.
3. `GenerateX.Fill` y llamarlo desde `Generate.Process`.
4. `CompareX` y registrarlo en `CompareDatabase`.
5. Incluir la colección en `Database.ToSqlDiff`.
6. Flag en `SqlOptionIgnore` y nodo `[SchemaNode]` para el árbol.

Añadir otro motor de base de datos:

1. Librerías Schema + Ui.
2. Implementar `IProjectHandler`, `IGenerator`, `IDatabaseComparer`, `IFront`.
3. Registrar el handler en `MainForm.LoadProjectHandlers`.

## 13. Referencias rápidas de código

| Tema | Ruta |
|---|---|
| Solución | `OpenDBDiff.sln` |
| Carga | `OpenDBDiff.SqlServer.Schema/Generates/Generate.cs` |
| Diff | `OpenDBDiff.SqlServer.Schema/Compare/CompareDatabase.cs` |
| Script | `OpenDBDiff.SqlServer.Schema/Model/Database.cs` |
| Estados | `OpenDBDiff.Abstractions.Schema/StatusEnum.cs` |
| GUI | `OpenDBDiff/UI/MainForm.cs` |
| Handler | `OpenDBDiff.SqlServer.Ui/Front/SQLServerProjectHandler.cs` |
| CLI | `OpenDBDiff.CLI/Program.cs` |
| Settings | `OpenDBDiff/Settings/Project.cs` |
| CI | `appveyor.yml` |

## 14. Diagramas

- Arquitectura: [diagramas/01-arquitectura.md](diagramas/01-arquitectura.md)
- Secuencia: [diagramas/02-secuencia.md](diagramas/02-secuencia.md)
- Flujo: [diagramas/03-flujo.md](diagramas/03-flujo.md)


## Tablas en memoria y selección

Desde esta implementación, `Table` conserva tipo de almacenamiento y durabilidad; `Index` conserva HASH y `BucketCount`. Las consultas incorporan los metadatos modernos desde SQL Server 2014. `DatabaseInfo.MajorVersion` utiliza la versión real, aunque el enum histórico de consultas termine en 2017; las capacidades de memoria no dependen de ese último valor del enum.

`MemoryOptimizedTableSql.Validate` revisa todos los objetos seleccionados antes de devolver fragmentos SQL. `ScriptGenerationException` reúne sus bloqueos. Las actualizaciones también prevalidan; no se autoriza por ello ninguna ejecución sobre bases reales del usuario.

Las pruebas MSTest incorporan casos de creación, eliminación, HASH, clonación, selección por nombre/tipo, versiones, requisitos, dependencias y errores de lectura. Los probes independientes en `OpenDBDiff.Tests/Integration/` complementan la suite con SQL Server local y WinForms. Instrucciones y límites: [Ejecucion.md](Ejecucion.md) y [TablasEnMemoria.md](TablasEnMemoria.md).
