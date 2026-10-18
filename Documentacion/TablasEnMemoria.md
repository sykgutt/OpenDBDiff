# Tablas optimizadas para memoria

## Comportamiento

El motor mantiene una sola colección `Database.Tables`. Cada tabla conserva `IsMemoryOptimized`, `Durability`, índices, claves y columnas. Los índices HASH conservan `BucketCount`, incluidas las claves primarias y únicas; estas claves no se duplican como índices independientes.

Schema presenta `Tables` y `Memory-Optimized Tables`, con formato **visibles / total**. Los filtros visuales no eliminan objetos del modelo ni pierden la selección. Se clasifica por la definición deseada; las tablas eliminadas conservan su tipo actual. `sysdiagrams` mantiene el tratamiento anterior. La suma de grupos es la unión de ambas bases, no necesariamente el número de tablas de una sola base.

La selección se resuelve por tipo y nombre completo, no solamente por ID: los IDs de índices se repiten entre tablas. Seleccionar una tabla incluye sus cambios completos; seleccionar solamente un hijo limita la generación a ese hijo. Como antes, una lista de selección vacía significa comparación completa.

## Lectura y errores

`GenerateTables` acepta el grupo de archivos nulo de las tablas en memoria. Las consultas modernas incorporan propiedades de `sys.tables` y HASH desde `sys.hash_indexes`; las versiones anteriores a 2014 conservan las consultas anteriores. Las consultas de índices ya no descartan los objetos en memoria por no tener un espacio de datos en disco.

Un error al leer tablas, columnas, restricciones o índices cancela la comparación con contexto de base y etapa/objeto. No se devuelve el catálogo parcial como resultado válido. Los filtros de comparación que excluyen metadatos esenciales deben habilitarse antes de generar cambios en tablas en memoria.

## Generación y bloqueos

`MemoryOptimizedTableSql` concentra las definiciones, diferencias y prevalidación utilizadas por GUI, CLI y actualizaciones. Las capacidades utilizan la versión mayor real del motor, `IsXTPSupported` y la presencia de un filegroup `MEMORY_OPTIMIZED_DATA` con contenedor en línea. No se crean rutas o contenedores automáticamente.

| Operación | Tratamiento |
|---|---|
| Crear/eliminar tabla en memoria | T-SQL específico desde SQL Server 2014, sujeto a requisitos y dependencias |
| Columnas, DEFAULT, restricciones e índices HASH/no agrupados | ALTER directo desde SQL Server 2016, según la operación |
| Cambiar solamente `BUCKET_COUNT` | `ALTER TABLE ... ALTER INDEX ... REBUILD WITH (BUCKET_COUNT=...)`; no recrea la tabla |
| Conversión disco/memoria, cambio de durabilidad o reconstrucción | Bloqueo con explicación; no copia ni migra datos automáticamente |
| Dependencia externa con schema binding, incluidos procedimientos nativos | Bloquea cambios estructurales incompatibles; un cambio exclusivo de DEFAULT no elimina ni altera la columna y puede continuar |
| Índice de memoria distinto de HASH/no agrupado | Bloqueo explícito; esta entrega no emite índices COLUMNSTORE de memoria |
| Nueva columna NOT NULL sin DEFAULT | Bloqueo para evitar una migración implícita de filas |

Las restricciones propias de la tabla, como CHECK, no se confunden con dependencias externas. Para índices en memoria se comparan únicamente las propiedades que forman parte de su definición emitida; opciones convencionales como `STATISTICS_NORECOMPUTE` no producen diferencias ni SQL imposible de representar. Los límites por versión incluyen la ausencia de ALTER de memoria en 2014, CHECK/FK desde 2016 y el límite de ocho índices antes de 2017. No se afirma soporte universal de cualquier característica nueva del servidor.

La prevalidación reúne todos los motivos de bloqueo de los objetos seleccionados antes de devolver SQL. La GUI conserva el árbol, marca `[script blocked]` y explica el motivo en el tooltip; el script queda vacío y las acciones de copiar/guardar se deshabilitan hasta generar una selección válida. Tanto guardar desde GUI como la CLI validan antes de abrir el archivo de salida. Las acciones de actualización prevalidan el lote antes de ejecutar el primer objeto.

## Evidencia y límites

En la validación local del 2026-10-17 se cargaron 219 tablas de CuriosityPOS y 229 de CuriosityPROD; ambas tienen 12 tablas en memoria. La comparación reunió 240 tablas: 228 convencionales y 12 en memoria. Son cifras observadas, no constantes del programa.

La revisión posterior aisló la diferencia de `[inMem].[Transaction]`: PROD tiene un DEFAULT `(getdate())` en `TrTimeStamp` y POS no. El procedimiento `[inMem].[ins_NativeinMem_TransactionVPH]` usa una lista explícita de columnas y no referencia `TrTimeStamp`; la generación permite quitar exclusivamente ese DEFAULT y mantiene el bloqueo para cambios estructurales. La comprobación fue de solo lectura y no aplicó el script. Se verificó el panel WinForms y la conservación de selección mediante un control instanciado y renderizado; no equivale a recorrer manualmente todos los diálogos de la aplicación.

Las pruebas de ejecución utilizaron **SQL Server 17.0.1145.4**, en bases desechables independientes. SQL Server 2014 y otras versiones se cubren con reglas y pruebas unitarias; no se probaron aquí con motores instalados de esas versiones. No se aplicó SQL sobre CuriosityPOS ni CuriosityPROD.

Fuentes de Microsoft: [sys.hash_indexes](https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-hash-indexes-transact-sql), [modificación de tablas en memoria](https://learn.microsoft.com/en-us/sql/relational-databases/in-memory-oltp/altering-memory-optimized-tables), [requisitos](https://learn.microsoft.com/en-us/sql/relational-databases/in-memory-oltp/requirements-for-using-memory-optimized-tables).

Instrucciones para repetir los probes y pruebas: [Ejecucion.md](Ejecucion.md).
