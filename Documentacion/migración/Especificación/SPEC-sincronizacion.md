# SPEC: sincronizacion

## Objetivo y contrato

Producir T-SQL que transforme actual/target en deseada/source y permitir las operaciones explícitas que ofrece la GUI. Entrada: modelo comparado, opciones, selección y, solo para aplicar, conexión target. Salida: SQLScriptList/texto o motivos de bloqueo; para aplicación, resultado/errores de ejecución. Depende de comparacion.

Fuentes: [Database.ToSqlDiff](../../../OpenDBDiff.SqlServer.Schema/Model/Database.cs), [SQLScript](../../../OpenDBDiff.Abstractions.Schema/SQLScript.cs), [MemoryOptimizedTableSql](../../../OpenDBDiff.SqlServer.Schema/Model/MemoryOptimizedTableSql.cs), [Updater](../../../OpenDBDiff/Updater.cs), [MainForm](../../../OpenDBDiff/UI/MainForm.cs).

## Requisitos

| ID | Contrato observado |
|---|---|
| SYN-01 | Emitir operaciones por estado y tipo, con orden de dependencias y ScriptAction. Lista de selección vacía representa generación completa; selección explícita limita el script |
| SYN-02 | Validar selección completa antes de devolver SQL de memoria. Agregar motivos distintos en ScriptGenerationException; no dejar script previo habilitado tras un bloqueo |
| SYN-03 | Conservar identidad tipo+nombre completo al seleccionar; marcar tabla incluye sus cambios; marcar solo hijo igual no incluye cambios ajenos |
| SYN-04 | Crear memoria con MEMORY_OPTIMIZED, DURABILITY, índices HASH/NO agrupados y claves ordenadas; no incluir filegroup/opciones propias de tablas de disco |
| SYN-05 | Cambiar solo buckets con ALTER TABLE ... ALTER INDEX ... REBUILD WITH (BUCKET_COUNT=...). Otros cambios soportados eliminan dependientes, modifican columnas y restauran constraints/índices |
| SYN-06 | Conservar generación y guardado separados de ejecución. La CLI nunca aplica SQL; GUI Updater recibe conexión derecha |
| SYN-07 | Prevalidar lote/objeto antes de actualización de memoria; subobjetos de memoria deben actualizarse a través de su tabla |
| SYN-08 | Guardar SQL solo después de generar/validar. Bloqueo previo conserva archivo de salida existente; no implica guardado atómico ante cualquier fallo de disco |

Cabeceras incluyen versión, usuario, máquina y fecha; no exigir igualdad byte por byte de esos campos variables. Las tablas convencionales pueden generar reconstrucciones con copia de datos. La presencia de `-- DIFF-ERROR ... Missing script` requiere revisión: no equivale a operación implementada.

## Matriz de bloqueos de memoria

| Condición | Resultado actual |
|---|---|
| Metadatos necesarios filtrados (tabla/índices/constraints y claves) | Bloquea para no generar desde definición incompleta |
| Target sin soporte In-Memory OLTP | Bloquea; creación requiere capacidades de SQL Server 2014+ |
| Crear/alterar sin grupo MEMORY_OPTIMIZED_DATA con contenedor online | Bloquea; no lo crea automáticamente |
| ALTER con cambios y versión menor a SQL Server 2016 | Bloquea |
| Conversión disco↔memoria, cambio de durabilidad, reconstrucción | Bloquea; no hay migración automática de datos |
| Dependientes schema-bound existentes | Bloquea cambios estructurales; excepción para cambios exclusivamente DEFAULT con estructura intacta |
| Durabilidad desconocida o SCHEMA_AND_DATA sin PK | Bloquea |
| Ningún índice; tipo distinto de HASH/nonclustered; buckets fuera de 1..1073741824 | Bloquea |
| Más de ocho índices en versión anterior a 2017 | Bloquea |
| CHECK/FK en memoria antes de 2016 | Bloquea |
| Reordenamiento que requiere reconstrucción, cambio identity/computed | Bloquea |
| Nueva columna NOT NULL sin DEFAULT explícito | Bloquea para preservar filas existentes |

Esta es la política implementada por el proyecto, no una matriz completa de capacidades del motor SQL Server. Un script permitido aún necesita pruebas de ejecución en la versión acordada.

## Aplicación directa: límites actuales

`Update all` prevalida, confirma y recorre los nodos principales actualmente presentes en el árbol. Su switch solo ejecuta estados **exactos Create y Alter**; no implementa todos los flags ni Drop. No es ejecución íntegra del SQL mostrado ni garantiza alcance independiente de filtros. La actualización individual también tiene rutas limitadas según tipo/estado.

Updater no proporciona transacción global ni backup automático. En rutas convencionales hay separación/reemplazo textual de `GO`; `rebuild` convencional muestra texto. GAP-02 registra estos límites para evitar reproducirlos como garantías. Si se reconstruye, aprobar un contrato nuevo de ejecución antes de ampliar lo que puede escribir.

## Aceptación

- **AC-SYN-01:** eliminar columna extra en generación completa; seleccionar una columna igual produce script vacío para ese cambio; no confundir IDs de índices de tablas distintas.
- **AC-SYN-02:** selección que excluye tabla de memoria bloqueada permite generar las tablas convencionales; incluirla devuelve todos los motivos y deshabilita copiar/guardar SQL.
- **AC-SYN-03:** crear/eliminar tabla de memoria y cambiar buckets produce sintaxis prevista; conversión y durabilidad fallan explícitamente.
- **AC-SYN-04:** dependencia nativa + cambio solo DEFAULT permitido; misma dependencia + cambio estructural bloqueado.
- **AC-SYN-05:** CLI bloqueada devuelve 1 y deja archivo existente sin modificación; CSV sigue disponible tras comparación válida con bloqueo SQL.
- **AC-SYN-06:** caracterizar Update/Update all por tipo, flags y filtro en un laboratorio autorizado antes de declarar paridad de ejecución.

Pruebas: [ColumnsTests](../../../OpenDBDiff.Tests/Model/ColumnsTests.cs), [MemoryOptimizedTests](../../../OpenDBDiff.Tests/Model/MemoryOptimizedTests.cs), [MemoryLab](../../../OpenDBDiff.Tests/Integration/MemoryLab.cs), [MemoryGuiProbe](../../../OpenDBDiff.Tests/Integration/MemoryGuiProbe.cs). Los probes independientes no son equivalentes a la suite MSTest ni se ejecutaron en esta sesión.
