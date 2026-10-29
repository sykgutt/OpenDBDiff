# SPEC: reportes-externo

## Objetivo, entradas y salidas

Exportar la comparación y facilitar revisión de definiciones. CSV recibe el modelo comparado y nombres source/target capturados al empezar; produce filas inmutables y archivo elegido por el usuario. Comparador externo recibe definiciones old/new, ejecutable y plantillas; produce archivos temporales y lanza el proceso. Depende de comparacion y configuracion-conexiones.

Fuentes: [DiffCsvReport](../../../OpenDBDiff/Export/DiffCsvReport.cs), [MainForm](../../../OpenDBDiff/UI/MainForm.cs), [ExternalDiff](../../../OpenDBDiff/ExternalDiff), [documentación CSV](../../ExportacionCsv.md).

## Requisitos CSV

| ID | Contrato observado |
|---|---|
| REP-01 | Export CSV entre External diff y Update all. Deshabilitado inicialmente/al iniciar nueva comparación; habilitado tras éxito aunque SQL esté bloqueado |
| REP-02 | Recorrer colecciones principales marcadas SchemaNode, no nodos visuales ni SQL. Incluir iguales, ignorar filtros visuales/casillas/expansión. No filas separadas de columnas/índices/constraints |
| REP-03 | Tipo = etiqueta de grupo; distinguir Tables/Memory-Optimized Tables. Deduplicar por ObjectType+FullName con comparación ordinal; ordenar por tipo, esquema y nombre ordinal |
| REP-04 | Sugerir `Diff<target>.csv`; sustituir caracteres inválidos por `_` solo en nombre sugerido. Rechazar source/target vacíos. Conservar instantánea tras cambiar selectores o modelo |
| REP-05 | Exactamente seis columnas, coma, UTF-8 BOM y CRLF; doblar comillas y entrecomillar campos con coma/comilla/CR/LF |
| REP-06 | SaveFileDialog con filtro/extensión CSV y confirmación de sobrescritura. Cancelar no escribe. Preparar contenido y escribir temporal en la misma carpeta; flush y Replace/Move al finalizar; limpiar temporal |
| REP-07 | Conservar archivo existente ante fallo de reemplazo; mostrar error mediante manejo GUI. El reporte no ejecuta las acciones descritas |

CSV incluye lo cargado por las opciones del motor. No recupera objetos excluidos antes de la comparación ni objetos ocultos por permisos SQL.

## Esquema de salida

| Posición / cabecera exacta | Valor |
|---|---|
| 1 Tipo de objeto | Etiqueta SchemaNode, o Memory-Optimized Tables |
| 2 Esquema | Owner; cadena vacía si no corresponde |
| 3 Nombre del objeto | Name sin esquema |
| 4 Nombre completo | FullName del modelo, p. ej. `[dbo].[Clientes]` |
| 5 Tipo de diferencia | Una de las cinco etiquetas inferiores |
| 6 Acción | Texto con nombres reales capturados |

Orden de clasificación: Original exacto → Create exacto → Drop exacto → contiene Rebuild → contiene AlterWhitespace → resto modificado. RebuildDependencies solo, sin Rebuild, usa la descripción general de cambio. No interpretar `new item` como traducción de la columna Acción.

En esta tabla `[Source]` y `[Target]` representan el nombre real rodeado de corchetes; no se sanea su contenido como archivo:

| Estado / etiqueta | Acción exacta |
|---|---|
| Create / `new item` | Crear en BD target [Target]: el objeto existe en BD source [Source] y falta en BD target. |
| Drop / `missing item` | Eliminar de BD target [Target]: el objeto existe en BD target y falta en BD source [Source]. |
| Rebuild / `changed item data` | Revisar la reconstrucción del objeto en BD target [Target] para coincidir con BD source [Source]. |
| AlterWhitespace / `changed item view` | Revisar diferencias de formato entre BD source [Source] y BD target [Target]; contenido equivalente según las opciones de comparación. |
| Otros modificados / `changed item data` | Actualizar la definición o estructura en BD target [Target] para coincidir con BD source [Source]. |
| Original / `unchanged item` | Sin acción: objeto sin cambios entre BD source [Source] y BD target [Target]. |

`changed item data` no compara filas. Los consumidores antiguos de cinco columnas deben admitir la sexta. Una comparación sin objetos produce solo cabecera. La exportación no deduce acciones nuevas ni garantiza que puedan ejecutarse.

## Comparador externo

| ID | Contrato observado |
|---|---|
| REP-08 | Seleccionado: dos archivos old/new, vacío para lado ausente. Todos: colecciones principales modificadas, omitiendo Original; carpetas por tipo y nombre saneado |
| REP-09 | Izquierda externa = actual/old; derecha externa = deseada/new. No copiar la orientación de selectores GUI |
| REP-10 | Soportar plantillas con `{left}`, `{right}`, `{leftTitle}`, `{rightTitle}` y presets WinMerge/Beyond Compare; iniciar ejecutable con UseShellExecute=false |
| REP-11 | Temporales en `%TEMP%\OpenDBDiff\extdiff\<sesión>\old` y `new`; limpiar otras sesiones cuando sea posible; comunicar ejecutable ausente o fallo de lanzamiento |

GAP-07: el recolector `SafeSql` devuelve null si ToSql falla y puede omitir objetos sin texto en ambos lados. El comparador externo no constituye un inventario exhaustivo de errores. No tiene la misma cobertura que CSV.

## Aceptación

- **AC-REP-01:** cinco estados y combinaciones reproducen etiquetas y seis textos; Rebuild gana a whitespace.
- **AC-REP-02:** tabla normal, memoria, rol sin esquema, igual y tipos distintos con mismo FullName; sin duplicados internos ni hijos como filas.
- **AC-REP-03:** Clone sin Name y cambio posterior de selectores conservan `DiffTargetReal.csv` y nombres de acciones.
- **AC-REP-04:** nombres con acentos, coma, comilla, salto y caracteres inválidos mantienen contenido; parser devuelve seis columnas, BOM y CRLF correctos.
- **AC-REP-05:** cancelar, sobrescribir y destino bloqueado; original intacto ante fallo y sin temporal residual.
- **AC-REP-06:** GUI filtrada/contraída y SQL bloqueado exporta la misma instantánea completa.
- **AC-REP-07:** seleccionado/todos externos mantienen old/new, placeholders y archivos ausentes; error de ejecutable visible.

Pruebas: [DiffCsvReportTests](../../../OpenDBDiff.Tests/Export/DiffCsvReportTests.cs), [CsvExportGuiProbe](../../../OpenDBDiff.Tests/Integration/CsvExportGuiProbe.cs), [ExternalDiffHelperTests](../../../OpenDBDiff.Tests/ExternalDiff/ExternalDiffHelperTests.cs). El test llamado `Save_RoundTripsFiveColumnsAndUsesUtf8BomAndCrlf` conserva un nombre antiguo pero verifica **seis** columnas; no usar su nombre como contrato. La regresión Clone comprueba pérdida de Name y contexto capturado, no todo el recorrido de ProgressForm con SQL real.
