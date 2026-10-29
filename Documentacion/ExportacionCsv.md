# Exportación de la comparación a CSV

Después de completar **Compare**, pulsar **Export CSV**, entre **External diff** y **Update all**. Elegir carpeta y confirmar el nombre en el diálogo. La sugerencia es `DiffNombreBaseDerecha.csv`: usa la base actual que recibiría los cambios. Se puede modificar el nombre; sobrescribir requiere confirmación.

## Alcance

El reporte es una instantánea de todos los objetos principales cargados en la última comparación, incluidos los iguales. Ignora filtros visuales, casillas y nodos contraídos. Las opciones del motor siguen determinando qué objetos se cargan y cómo se comparan; el reporte no realiza otra consulta del catálogo.

Cada tabla, vista, procedimiento, función u otro objeto de primer nivel ocupa una fila. Las columnas, índices, restricciones y demás hijos no tienen filas independientes. Los tipos usan las etiquetas del árbol; las tablas se separan en `Tables` y `Memory-Optimized Tables`. El esquema queda vacío cuando no corresponde.

Cambiar los selectores de conexión después de comparar no modifica el reporte ni el nombre sugerido. Al iniciar otra comparación se descarta la instantánea; si falla, la exportación queda deshabilitada. Un bloqueo de generación SQL no impide exportar una comparación completada: el CSV no genera ni aplica T-SQL.

## Columnas y estados

Orden exacto: `Tipo de objeto`, `Esquema`, `Nombre del objeto`, `Nombre completo`, `Tipo de diferencia`, `Acción`. Los consumidores que esperen cinco columnas deben admitir la sexta. El nombre completo conserva el del modelo, por ejemplo `[dbo].[Clientes]`.

| Estado del objeto | Tipo de diferencia |
|---|---|
| Creado, verde | `new item` |
| Faltante, rojo | `missing item` |
| Formato, dorado (`AlterWhitespace`) | `changed item view` |
| Estructura/definición, azul; reconstrucción morada; otros estados modificados | `changed item data` |
| Sin cambios | `unchanged item` |

Los estados combinados siguen la prioridad del árbol: reconstrucción, formato, otros cambios. `changed item view` es una etiqueta de formato, no significa que el objeto sea una vista SQL. `changed item data` no representa diferencias entre registros. Se exporta la clasificación del motor, sin volver a comparar textos ni cambiar sus opciones de espacios o mayúsculas.

## Formato y escritura

Separador coma, UTF-8 con BOM y terminadores CRLF. Las comas, comillas y saltos de línea de los nombres se escapan como campos CSV. Las filas se ordenan por tipo, esquema y nombre con comparación ordinal. Los caracteres inválidos de Windows se sustituyen por `_` solamente en el nombre sugerido del archivo.

Se prepara el contenido antes de escribir; un temporal en la misma carpeta se reemplaza o mueve al destino al finalizar. Un fallo no debe truncar un destino existente y el temporal se limpia. Cancelar no crea ni modifica archivos. Los errores se muestran mediante el manejo existente de la aplicación.

## Implementación y validación

`OpenDBDiff/Export/DiffCsvReport.cs` no depende de WinForms: recorre las propiedades `SchemaNode` del modelo sin entrar en hijos y captura los valores del reporte. La interfaz crea la instantánea al finalizar una comparación. Se registraron los archivos nuevos en los `.csproj` clásicos, sin paquetes NuGet nuevos.

`DiffCsvReportTests` cubre estados combinados, orden, instantánea, ausencia de duplicados y subobjetos, tablas en memoria, nombres sin esquema, codificación y lectura CSV de seis columnas, sobrescritura y fallo con destino bloqueado.

`OpenDBDiff.Tests/Integration/CsvExportGuiProbe.cs` usa modelos sintéticos, omite la configuración del usuario y no se conecta a SQL Server. Verifica posición y desplazamiento del botón, habilitación independiente del script, cancelación, filtros/casillas, nodos contraídos, nombre capturado y limpieza del estado al fallar otra comparación. Sustituye el diálogo por respuestas de prueba: comprueba sus propiedades y el evento real del botón; no automatiza la interacción nativa del selector de archivos.

Desde la raíz, después de compilar Debug y con `TestResults/` existente:

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo `
    '/out:TestResults\CsvExportGuiProbe.exe' '/r:System.Windows.Forms.dll' '/r:System.Drawing.dll' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.exe' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.SqlServer.Schema.dll' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.Abstractions.Schema.dll' `
    '.\OpenDBDiff.Tests\Integration\CsvExportGuiProbe.cs'
if ($LASTEXITCODE -ne 0) { throw 'Fallo al compilar CsvExportGuiProbe' }
& .\TestResults\CsvExportGuiProbe.exe '.\OpenDBDiff\bin\Debug' '.\TestResults\CsvExport'
if ($LASTEXITCODE -ne 0) { throw 'Fallo en CsvExportGuiProbe' }
```

El probe guarda `gui-export.csv` y `csv-export-panel.png` en su carpeta de salida. Resultados: [Seguimiento.md](Seguimiento.md).

## Acción y nombre de archivo

Los nombres source (izquierda) y target (derecha) se capturan al iniciar la comparación. Así se evita que la copia del modelo, que no conserva Name, produzca `Diff.csv`. El nombre target vacío se rechaza. La columna Acción describe crear, eliminar, actualizar, revisar formato, ninguna acción o revisar reconstrucción, e incluye los nombres reales de ambas bases. Los textos son recomendaciones y no ejecutan SQL.
