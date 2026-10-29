# Trazabilidad, aceptación y brechas

Revisión estática: 2026-10-29, commit 4d36338. S = fuente inspeccionada; H = resultado histórico en Seguimiento; P = validación pendiente. Ninguna fila significa prueba nueva ejecutada. Los nombres de tests sirven para localizar evidencia, no para afirmar cobertura exhaustiva.

## Requisitos → implementación → pruebas

| Requisitos | Implementación | Pruebas/escenarios y límite | Evidencia |
|---|---|---|---|
| MOD-01, MOD-02, MOD-03, MOD-04 | Generate.Process, Database, SchemaList, SQLQueries | AC-MOD-01/05; ampliar fixtures por familia y versión | S/P |
| MOD-05, MOD-07 | Table, Index, DatabaseInfo | MemoryOptimizedTests: TableMustPreserveMemoryOptimizedMetadata, ClonePreservesMemoryAndHashMetadata, VersionCapabilitiesUseRealMajorVersion | S/H; laboratorio por versión P |
| MOD-06 | GenerateTables.Fill, Generate.Process (índices) | TableReaderFailsInsteadOfReturningPartialCatalog | S/H; otros lectores GAP-01 |
| CFG-01, CFG-02, CFG-03 | SqlServerConnectFront, Project, MainForm | AC-CFG-01; CRUD/selector y persistencia de opciones GAP-08 | S/P |
| CFG-04, CFG-05 | SettingsDatabase.Open/Dispose | SettingsDatabaseTests: lectura, escritura, respaldo, entrada inválida, fallo de reemplazo | S/H |
| CFG-06 | SqlConnectionStringCompatibility.Parse | Parse_SpacedTrustCertificateAlias_PreservesConnectionValues; Parse_StandardKeyword_PreservesFalseValue | S/H |
| CFG-07 | SqlOption* | Inventario de defaults; AC-CFG-05 y matriz completa de combinaciones pendientes | S/P |
| CFG-08 | ExternalDiffSettings | Helpers de plantillas; persistencia completa AC-CFG-01/AC-REP-07 | S/P |
| CMP-01, CMP-03, CMP-06 | CompareDatabase, CompareBase, comparadores | ColumnsTests y MemoryOptimizedTests; ampliar familias convencionales | S/H/P |
| CMP-02 | StatusEnum, SchemaTreeView, DiffCsvReport | Report_MapsAllStatusesAndMatchesTreePrecedence | S/H |
| CMP-04 | Code, FormatCode, comparadores de código | FormatCodeTests; literales/semántica GAP-05 | S/H/P |
| CMP-05 | MemoryOptimizedTableSql.SameDefinition | EqualMemorySchemasHaveNoDifferences, MemoryIndexIgnoresDiskOnlyStatisticsOption, DefaultLiteralCaseIsPreservedEvenWithCaseInsensitiveIdentifiers | S/H |
| SYN-01, SYN-03 | Database.ToSqlDiff, selección de memoria | ColumnsTests; SelectingUnchangedChildDoesNotApplyOtherChanges; SameIndexIdOnDifferentTablesDoesNotLeakIntoSelection | S/H |
| SYN-02, SYN-04, SYN-05 | MemoryOptimizedTableSql | MissingDestinationRequirementsAreAggregated; ChangedHashIsDetectedAndUsesAlterTable; SchemaBoundDependencyDoesNotBlockDefaultOnlyChange; MemoryLab | S/H; otros motores P |
| SYN-06, SYN-07 | Updater, MainForm | AC-SYN-06; alcance real de Update all GAP-02 | S/P |
| SYN-08 | MainForm.btnSaveAs, CLI.Work | CLI bloqueada en MemoryLab; fallo general de escritura/cancelación SQL GAP-09 | S/H/P |
| REP-01, REP-06, HOST-02, HOST-05 | MainForm, CsvExportGuiProbe | Probe sintético: cancelación/exportación, filtros, panel, bloqueo SQL; diálogo nativo P | S/H/P |
| REP-02, REP-03 | DiffCsvReport constructor | IncludesTopLevelObjects..., DeduplicatesWithinType..., UsesOrdinalOrder... | S/H |
| REP-04 | MainForm.StartComparison, DiffCsvReport | UsesCapturedNamesWhenDatabaseCloneLosesName, RejectsEmptyTargetName, SanitizesOnlyTheSuggestedFilename | S/H; recorrido SQL real P |
| REP-05, REP-07 | DiffCsvReport.Save/Escape | RoundTripsFiveColumns... (asserts seis), EscapesRealDatabaseNames..., ReplacesExistingFile..., LockedDestination... | S/H |
| REP-08, REP-09, REP-10, REP-11 | ExternalDiffObjectCollector/Launcher/Arguments | ExternalDiffHelperTests, AC-REP-07; proceso externo y fallos SafeSql P | S/H/P |
| HOST-01, HOST-03, HOST-04, HOST-06 | MainForm, SchemaTreeView, ProgressForm | MemoryGuiProbe y CsvExportGuiProbe parciales; recorrido manual completo P | S/H/P |
| HOST-07, HOST-08, HOST-09 | CLI Program/CommandlineOptions | AC-HOST-05/06 y MemoryLab parcial; parámetros/ayuda completos P | S/H/P |
| DATA-01, DATA-02, DATA-03, DATA-04, DATA-05, DATA-06 | DataCompareForm, Updater | AC-DATA-01..05 propuestos; sin tests dedicados identificados | S/P |

Enlaces a archivos de prueba y conteos en [Inventario](Inventario.md); enlaces a implementaciones en cada SPEC. Para un requisito compartido, probar tanto proveedor como host consumidor.

## Brechas: no convertir en requisitos de réplica

| ID | Hallazgo y evidencia estática | Acción antes de reconstruir |
|---|---|---|
| GAP-01 | Lectores Views/Triggers/UserDataTypes/ExtendedProperties capturan y agregan mensajes sin abortar. Generate.Process no convierte toda esa lista en fallo | Definir política uniforme de catálogo incompleto y probarla por lector |
| GAP-02 | Update all solo ejecuta Create/Alter exactos sobre árbol; Updater convencional manipula GO como texto y no es transacción global | Definir operaciones soportadas, confirmación, atomicidad y recuperación; no asumir paridad con ToSqlDiff |
| GAP-03 | Datos auxiliares usa SELECT *, posiciones, Merge y CommandBuilder sin suite dedicada | Decidir alcance; fixtures de claves, columnas y concurrencia antes de afirmar sincronización segura |
| GAP-04 | Cobertura por familia/versión, permisos, rendimiento, DPI y diálogos nativos incompleta | Acordar matriz de soporte y métricas; no inventar garantías |
| GAP-05 | Code elimina whitespace con regex; filtros usan traducción propia a regex | Caracterizar literales y patrones; separar compatibilidad histórica de semántica deseada |
| GAP-06 | Defaults tiene colección/etiqueta pero no etapa propia de lectura/comparación/emisión en orquestadores inspeccionados | Determinar si completar soporte o declarar fuera del alcance efectivo; no confundir DEFAULT de columna con objeto Default |
| GAP-07 | ExternalDiffObjectCollector.SafeSql atrapa errores y devuelve null | Definir visibilidad de fallos y prueba de objetos omitidos |
| GAP-08 | Project.Options tiene BsonIgnore, selección restaura conexiones; Upsert puede fallar sin reporte al host | Decidir contrato de persistencia de opciones y manejo de fallo; crear regresiones |
| GAP-09 | Guardar SQL no comprueba DialogResult, solo FileName; CSV sí comprueba cancelación | Reproducir cancelar con ruta anterior en entorno aislado y corregir por separado si se autoriza |

Estos hallazgos no se corrigieron en la tarea documental. No todos están reproducidos en ejecución; «riesgo» no equivale a incidente confirmado. Antes de cerrar una brecha, registrar caso reproducible, decisión, prueba y commit.

## Plantilla de evidencia futura

Para cada AC o GAP registrar: requisito, commit, fecha, configuración, SO/runtime/arquitectura, versión SQL si aplica, entrada/fixture sin secretos, comando, resultado esperado/observado, log/TRX o captura, limpieza y limitaciones. En SQL, distinguir análisis estático, generación, ejecución en laboratorio y producción. El baseline de 72 pruebas históricas no cubre todos los contratos de esta matriz.
