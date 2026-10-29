# Inventario verificable de la baseline

Generado de fuentes del checkout 4d36338 el 2026-10-29. Inventario estático; no certifica ejecución ni soporte exhaustivo. Rutas relativas a la raíz del repositorio.

## Proyectos y referencias

| Proyecto | Target | Referencias a proyectos |
|---|---|---|
| [OpenDBDiff](../../../OpenDBDiff/OpenDBDiff.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema, OpenDBDiff.Abstractions.Ui, OpenDBDiff.SqlServer.Schema, OpenDBDiff.SqlServer.Ui |
| [OpenDBDiff.Abstractions.Schema](../../../OpenDBDiff.Abstractions.Schema/OpenDBDiff.Abstractions.Schema.csproj) | v4.5.2 | — |
| [OpenDBDiff.Abstractions.Ui](../../../OpenDBDiff.Abstractions.Ui/OpenDBDiff.Abstractions.Ui.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema |
| [OpenDBDiff.CLI](../../../OpenDBDiff.CLI/OpenDBDiff.CLI.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema, OpenDBDiff.SqlServer.Schema |
| [OpenDBDiff.SqlServer.Schema](../../../OpenDBDiff.SqlServer.Schema/OpenDBDiff.SqlServer.Schema.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema |
| [OpenDBDiff.SqlServer.Ui](../../../OpenDBDiff.SqlServer.Ui/OpenDBDiff.SqlServer.Ui.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema, OpenDBDiff.Abstractions.Ui, OpenDBDiff.SqlServer.Schema |
| [OpenDBDiff.Tests](../../../OpenDBDiff.Tests/OpenDBDiff.Tests.csproj) | v4.5.2 | OpenDBDiff.Abstractions.Schema, OpenDBDiff.SqlServer.Schema |

GUI y CLI usan AnyCPU con Prefer32Bit=false. La carga nativa de Scintilla debe probarse en la arquitectura efectiva. Abstractions.Ui expone Control/DockStyle/AnchorStyles, por lo que su nombre no implica independencia de WinForms.

## Paquetes declarados

| Proyecto | Paquete | Versión | targetFramework de packages.config |
|---|---|---|---|
| OpenDBDiff | DiffPlex | 1.6.3 | net452 |
| OpenDBDiff | Essy.Tools.InputBox | 1.0.0 | net452 |
| OpenDBDiff | jacobslusser.ScintillaNET | 3.6.3 | net452 |
| OpenDBDiff | LiteDB | 5.0.9 | net452 |
| OpenDBDiff.CLI | CommandLineParser | 2.8.0 | net452 |
| OpenDBDiff.SqlServer.Schema | TSQL.Parser | 1.5.2 | net452 |
| OpenDBDiff.Tests | Castle.Core | 4.4.1 | net452 |
| OpenDBDiff.Tests | LiteDB | 5.0.9 | net452 |
| OpenDBDiff.Tests | Moq | 4.7.145 | net452 |
| OpenDBDiff.Tests | System.Runtime.CompilerServices.Unsafe | 4.7.1 | net452 |
| OpenDBDiff.Tests | System.Threading.Tasks.Extensions | 4.5.4 | net452 |
| OpenDBDiff.Tests | System.ValueTuple | 4.5.0 | net452 |

Las referencias QualityTools de MSTest proceden de Visual Studio y no aparecen como paquete NuGet. El proyecto GUI también referencia System.Deployment; inspeccionar uso real antes de sustituir APIs al migrar a .NET moderno.

## Grupos principales del modelo

| Colección | Tipo de elemento | Etiqueta exacta del árbol/CSV |
|---|---|---|
| FullText | FullText | Full Text Catalog |
| TablesTypes | TableType | Table Type |
| PartitionSchemes | PartitionScheme | Partition Scheme |
| PartitionFunctions | PartitionFunction | Partition Functions |
| Defaults | Default | Defaults |
| Roles | Role | Roles |
| Functions | Function | Functions |
| Users | User | Users |
| Views | View | Views |
| Assemblies | Assembly | Assemblies |
| Synonyms | Synonym | Synonyms |
| DDLTriggers | Trigger | DLL Triggers |
| FileGroups | FileGroup | File Groups |
| Rules | Rule | Rules |
| Procedures | StoredProcedure | Stored Procedures |
| CLRProcedures | CLRStoredProcedure | CLR Stored Procedures |
| CLRFunctions | CLRFunction | CLR Functions |
| Schemas | Schema | Schemas |
| XmlSchemas | XMLSchema | XML Schemas |
| Tables | Table | Tables |
| UserTypes | UserDataType | User Types |

Memory-Optimized Tables es un grupo adicional del árbol/CSV derivado de Tables.IsMemoryOptimized. La etiqueta histórica DLL Triggers se conserva tal como está en SchemaNode. Defaults tiene colección pero no una etapa propia en la orquestación principal; véase GAP-06.

## Flags de lectura/comparación

Fuente: [SqlOptionIgnore](../../../OpenDBDiff.SqlServer.Schema/Options/SqlOptionIgnore.cs). Estos valores describen new SqlOption(), cuyo defaultValue es true. True habilita muchas comparaciones/lecturas: no invertirlos por el nombre Ignore.

| Propiedad | Valor inicial con SqlOption() |
|---|---|
| FilterPartitionFunction | true |
| FilterPartitionScheme | true |
| FilterIndexFilter | true |
| FilterIndex | true |
| FilterConstraintPK | true |
| FilterConstraintFK | true |
| FilterConstraintUK | true |
| FilterConstraintCheck | true |
| FilterIndexFillFactor | true |
| FilterIndexIncludeColumns | true |
| FilterIndexRowLock | true |
| FilterColumnOrder | true |
| FilterColumnIdentity | true |
| FilterColumnCollation | true |
| FilterNotForReplication | true |
| FilterUsers | true |
| FilterRoles | true |
| FilterCLRFunction | true |
| FilterCLRTrigger | true |
| FilterCLRUDT | true |
| FilterCLRStoredProcedure | true |
| FilterFullText | true |
| FilterFullTextPath | false |
| FilterTableLockEscalation | true |
| FilterTableChangeTracking | true |
| FilterConstraint | true |
| FilterFunction | true |
| FilterStoredProcedure | true |
| FilterView | true |
| FilterTable | true |
| FilterTableOption | true |
| FilterUserDataType | true |
| FilterTrigger | true |
| FilterSchema | true |
| FilterXMLSchema | true |
| FilterTableFileGroup | true |
| FilterExtendedProperties | true |
| FilterDDLTriggers | true |
| FilterSynonyms | true |
| FilterRules | true |
| FilterAssemblies | true |

## Otros bloques de opciones

| Bloque | Clave/propiedad | Inicialización observada |
|---|---|---|
| Comparison | IgnoreWhiteSpacesInCode | false |
| Comparison | CaseSensityInCode | CaseInsensity (1) |
| Comparison | CaseSensityType | Automatic (0, valor inicial enum) |
| Comparison | ReloadComparisonOnUpdate | false (valor inicial bool) |
| Script | AlterObjectOnSchemaBinding | true |
| Defaults | defaultIntegerValue | `"0"` |
| Defaults | defaultRealValue | `"0.0"` |
| Defaults | defaultTextValue | `"''"` |
| Defaults | defaultDateValue | `"getdate()"` |
| Defaults | defaultVariantValue | `"''"` |
| Defaults | defaultNTextValue | `"N''"` |
| Defaults | defaultBlobValue | `"0x"` |
| Defaults | defaultUniqueValue | `"NEWID()"` |
| Defaults | useDefaultValueIfExists | `true` |
| Defaults | defaultTime | `"00:00:00"` |
| Defaults | defaultXml | `""` |

Filters.Items inicial:
- Table: `dbo.dtproperties`
- Assembly: `Microsoft.SqlServer.Types`
- Schema: `db_accessadmin`
- Schema: `db_backupoperator`
- Schema: `db_datareader`
- Schema: `db_datawriter`
- Schema: `db_ddladmin`
- Schema: `db_denydatareader`
- Schema: `db_denydatawriter`
- Schema: `db_owner`
- Schema: `db_securityadmin`
- Schema: `guest`
- Schema: `INFORMATION_SCHEMA`
- Schema: `sys`

El patrón se aplica al Name o al Owner para esquemas según SqlOptionFilterItem; no deducir eficacia de un patrón histórico sin fixture (por ejemplo, dbo.dtproperties).

## Métodos de prueba incluidos

Conteo estático de [TestMethod] en archivos Compile del proyecto de tests. No se ejecutaron aquí. Los probes Integration se compilan/ejecutan por separado según Ejecucion.md.

| Archivo | Métodos |
|---|---|
| [ByteToHexEncoderTests.cs](../../../OpenDBDiff.Tests/Generates/Util/ByteToHexEncoderTests.cs) | 1 |
| [ColumnsTests.cs](../../../OpenDBDiff.Tests/Model/ColumnsTests.cs) | 3 |
| [MemoryOptimizedTests.cs](../../../OpenDBDiff.Tests/Model/MemoryOptimizedTests.cs) | 28 |
| [FormatCodeTests.cs](../../../OpenDBDiff.Tests/Model/FormatCodeTests.cs) | 10 |
| [ExternalDiffHelperTests.cs](../../../OpenDBDiff.Tests/ExternalDiff/ExternalDiffHelperTests.cs) | 10 |
| [DiffCsvReportTests.cs](../../../OpenDBDiff.Tests/Export/DiffCsvReportTests.cs) | 12 |
| [SettingsDatabaseTests.cs](../../../OpenDBDiff.Tests/Settings/SettingsDatabaseTests.cs) | 6 |
| [SqlConnectionStringCompatibilityTests.cs](../../../OpenDBDiff.Tests/Settings/SqlConnectionStringCompatibilityTests.cs) | 2 |

Total detectado: **72 métodos**, contando también atributos TestMethod(). Comparar contra el número descubierto por vstest al migrar; un inventario estático no acredita que todos se descubran o pasen.

## Fuente específica para ampliar caracterización

- Lectores: `OpenDBDiff.SqlServer.Schema/Generates/` y `SQLQueries/`.
- Comparadores: `OpenDBDiff.SqlServer.Schema/Compare/`.
- Propiedades/emisión de cada tipo: `OpenDBDiff.SqlServer.Schema/Model/`.
- Contratos compartidos: `OpenDBDiff.Abstractions.Schema/` y `OpenDBDiff.Abstractions.Ui/`.
- Flujos y diálogos: `OpenDBDiff/UI/` y `OpenDBDiff.SqlServer.Ui/Front/`.

Antes de reconstruir una familia, inventariar sus propiedades comparadas y DDL con fixtures; este índice no afirma haber verificado cada rama de todos los tipos.
