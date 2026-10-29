# SPEC: lectura-modelo

## Objetivo y contrato

Representar el catálogo de una base SQL Server con identidad, propiedades y dependencias suficientes para comparar y generar SQL. Entrada: cadena de conexión y `SqlOption`. Salida: `Database` con colecciones, `Info`, opciones y objetos; eventos de progreso o error. Consumidores: comparación, GUI/CLI y auxiliares.

Fuentes: [Generate](../../../OpenDBDiff.SqlServer.Schema/Generates/Generate.cs), [Database](../../../OpenDBDiff.SqlServer.Schema/Model/Database.cs), [contratos](../../../OpenDBDiff.Abstractions.Schema/Model), [lectores](../../../OpenDBDiff.SqlServer.Schema/Generates), [consultas](../../../OpenDBDiff.SqlServer.Schema/SQLQueries). Inventario de grupos y opciones: [Inventario](Inventario.md).

## Requisitos

| ID | Contrato observado |
|---|---|
| MOD-01 | Cargar mediante ADO.NET y consultas por versión, sin SMO. Mantener los filtros/opciones efectivos y la información de versión/collation |
| MOD-02 | Cada objeto conserva tipo, nombre, propietario/esquema cuando corresponda, nombre completo delimitado, estado y relación con padre/raíz. Los objetos sin esquema no deben recibir dbo artificialmente |
| MOD-03 | Mantener colecciones principales y subobjetos: tablas con columnas/índices/restricciones/triggers; código con definición y dependencias; propiedades extendidas donde el lector las proporcione |
| MOD-04 | Construir dependencias tras la lectura. Resolver objetos por nombre completo/ID según los contratos existentes; no usar un ID local como identidad global entre familias |
| MOD-05 | Tablas en memoria conservan IsMemoryOptimized, Durability, OriginalTable y dependientes enlazados al esquema; índices HASH conservan BucketCount y orden de claves |
| MOD-06 | Leer índices de memoria con data_space_id=0 sin convertir DBNull a string. Fallos en tablas/columnas/restricciones e índices abortan con contexto de base/etapa/objeto |
| MOD-07 | Las capacidades de memoria usan MajorVersion real, no el último valor del enum histórico. Mantener las ramas específicas de Azure existentes sin prometer equivalencia con todas las variantes Azure |

La identidad para selección y deduplicación requiere tipo más nombre completo; la sensibilidad de nombres depende del contexto. `Database.Clone` recorre colecciones pero no conserva `Name`; su `AllObjects` se asigna desde el original. No asumir copia profunda íntegra. El CSV evita depender de ese nombre mediante contexto capturado.

## Objetos y límites

El inventario enumera las 21 propiedades con `SchemaNode`, incluida la etiqueta heredada `DLL Triggers` para DDLTriggers. Tables y Memory-Optimized Tables comparten `Database.Tables`. Estar en el modelo no acredita lectura/comparación/emisión completas: `Defaults` existe como colección, pero no aparece como etapa propia en `Generate.Process`, `CompareDatabase` ni `Database.ToSqlDiff`. Conservar esta distinción para GAP-06.

SQL Server 2005+ es la declaración histórica del producto, no una matriz certificada aquí. El enum contiene versiones antiguas y Azure; las pruebas de memoria realizadas previamente no cubren ejecución en todos esos motores.

## Errores y brechas

GAP-01: `GenerateViews`, `GenerateTriggers`, `GenerateUserDataTypes` y `GenerateExtendedProperties` todavía agregan `MessageLog` en capturas sin abortar; `GenerateFileGroups`, en cambio, relanza su excepción. Por tanto, no afirmar que **todo** error de catálogo siempre aborta. El resultado deseado para una reconstrucción es hacer visible la incompletitud antes de proponer cambios; su política concreta debe aprobarse. No cambiarla incidentalmente en el retarget.

La visibilidad del catálogo depende de permisos; ausencia de objeto observado no demuestra ausencia física. No se han certificado mínimos de permisos por versión.

## Aceptación

- **AC-MOD-01:** dado un catálogo sintético con tabla, vista y objeto sin esquema, preservar tipo/nombre/esquema y acceso a hijos sin duplicarlos como objetos principales.
- **AC-MOD-02:** dada tabla de memoria con HASH y clave compuesta, leer/clonar conservando durabilidad, buckets y orden; no emitir opciones de disco.
- **AC-MOD-03:** al fallar el lector de tablas, propagar error contextual y no presentar esa lectura como comparación válida.
- **AC-MOD-04:** con versiones reales 12, 13 y 14, seleccionar capacidades de memoria correspondientes aunque el enum no contenga versiones nuevas.
- **AC-MOD-05:** caracterizar por separado los lectores con GAP-01 y la colección Defaults; marcar pendiente hasta disponer de fixtures y laboratorio.

Pruebas existentes: [MemoryOptimizedTests](../../../OpenDBDiff.Tests/Model/MemoryOptimizedTests.cs), especialmente `TableReaderFailsInsteadOfReturningPartialCatalog`, `ClonePreservesMemoryAndHashMetadata` y `VersionCapabilitiesUseRealMajorVersion`. Las variantes generales de catálogo necesitan ampliar caracterización.
