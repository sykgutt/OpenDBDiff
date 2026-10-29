# SPEC: comparacion

## Objetivo y contrato

Entrada: modelo actual (`origin`) y deseado (`destination`), cargados con opciones. Salida: modelo actual **mutado** con objetos/estados que describen la transformación al deseado. Dependencia: lectura-modelo. No compara registros de tablas en este flujo.

Fuentes: [CompareDatabase](../../../OpenDBDiff.SqlServer.Schema/Compare/CompareDatabase.cs), [CompareBase](../../../OpenDBDiff.SqlServer.Schema/Compare/CompareBase.cs), [comparadores especializados](../../../OpenDBDiff.SqlServer.Schema/Compare), [Code](../../../OpenDBDiff.SqlServer.Schema/Model/Code.cs), [estados](../../../OpenDBDiff.Abstractions.Schema/StatusEnum.cs).

## Requisitos

| ID | Contrato observado |
|---|---|
| CMP-01 | Objeto solo en deseada → Create; solo en actual → Drop; en ambas → comparador de su familia; igual → Original |
| CMP-02 | Conservar flags de modificación: Alter, AlterBody, AlterWhitespace, Rebuild, RebuildDependencies y estados especializados. No reducir el modelo interno a cinco etiquetas CSV |
| CMP-03 | Comparar familias registradas en CompareDatabase; tablas delegan columnas, índices, constraints, triggers y opciones. Conservar SourceInfo de la deseada |
| CMP-04 | Respetar opciones de sensibilidad y whitespace del código; clasificación de formato no es una garantía de equivalencia semántica SQL |
| CMP-05 | Tablas de memoria comparan solo propiedades representables, incluidos durabilidad, columnas, claves y buckets; no generar cambios por opciones exclusivas de disco |
| CMP-06 | Preservar definiciones originales necesarias para diff y SQL, además del resultado. No volver a utilizar origin como si fuera un modelo intacto |

## Estados visibles

| Estado | Árbol / reporte |
|---|---|
| Original | Sin cambio / unchanged item |
| Create exacto | Verde / new item |
| Drop exacto | Rojo / missing item |
| Incluye Rebuild | Morado / changed item data |
| Incluye AlterWhitespace, sin Rebuild | Dorado / changed item view |
| Otros modificados | Azul u otra representación especializada / changed item data |

La precedencia de formato se aplica al estado que ya produjo el motor; no se recalcula desde el CSV. No asumir que todas las familias detectan formato de idéntica manera. `Code.Compare`/`CompareExceptWhitespace` eliminan whitespace con regex cuando corresponde; esto incluye espacios dentro de literales. Es una limitación a caracterizar (GAP-05), no equivalencia SQL probada.

El motor y el árbol usan nombres/estados; la selección de generación es una dimensión distinta. Una reconstrucción puede producir modelos inmutables si conserva los resultados observables y sus contextos; una migración de framework debe mantener el algoritmo actual.

## Errores y aceptación

CompareDatabase envuelve excepciones en SchemaException. Un error no debe habilitar una nueva exportación CSV como si la comparación hubiera terminado.

- **AC-CMP-01:** dos modelos con objetos exclusivo-source, exclusivo-target, común igual y común modificado producen Create, Drop, Original y modificación respectivamente.
- **AC-CMP-02:** repetir sobre copias frescas con distintas opciones de whitespace/case conserva los resultados definidos por cada opción; registrar casos con literales como limitación.
- **AC-CMP-03:** cambio exclusivo de BucketCount se detecta; IsAutoStatistics de disco no produce diferencia falsa en memoria.
- **AC-CMP-04:** flags Rebuild+AlterWhitespace se muestran/exportan con prioridad de reconstrucción; Alter+AlterWhitespace con prioridad de formato.
- **AC-CMP-05:** no invertir source/target en GUI, CLI, exportación ni aplicación.

Pruebas: [ColumnsTests](../../../OpenDBDiff.Tests/Model/ColumnsTests.cs), [FormatCodeTests](../../../OpenDBDiff.Tests/Model/FormatCodeTests.cs), [MemoryOptimizedTests](../../../OpenDBDiff.Tests/Model/MemoryOptimizedTests.cs), [DiffCsvReportTests](../../../OpenDBDiff.Tests/Export/DiffCsvReportTests.cs). FormatCodeTests prueba normalización de snippets; no cubre por sí sola todos los comparadores ni equivalencia semántica.
