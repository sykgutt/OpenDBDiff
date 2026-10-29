# Exportar comparación a CSV

Plan aprobado: botón Export CSV entre External diff y Update all; todos los objetos principales, incluidos iguales; cinco columnas y estados del árbol; sugerencia Diff<BaseDerecha>.csv; guardado seguro e independiente de la generación SQL. Sin consultas o cambios SQL nuevos.

- [x] Crear pruebas de regresión para clasificación, datos, nombres, formato y escritura.
- [x] Implementar instantánea del modelo, sin subobjetos ni generación SQL.
- [x] Integrar botón, ciclo de habilitación y diálogo de ruta/sobrescritura.
- [x] Verificar CSV de cinco columnas con parser independiente y destino bloqueado.
- [x] Verificar WinForms con filtros, casillas, cancelación y nodos contraídos.
- [x] Revisar imagen del panel y completar build y suite: 69/69.
- [x] Documentar uso, límites, comandos de reproducción y seguimiento.

Detalles: [ExportacionCsv](../Documentacion/ExportacionCsv.md). Evidencia: [Seguimiento](../Documentacion/Seguimiento.md), entrada 2026-10-28. Se conserva el registro anterior de tablas en memoria en `plan.md` y `todo.md`.
