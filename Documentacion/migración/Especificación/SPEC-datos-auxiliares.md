# SPEC: datos-auxiliares

## Objetivo y alcance observado

Consultar dos tablas equivalentes seleccionadas y preparar cambios de filas de izquierda/source hacia derecha/target. Es una función auxiliar con escritura real: **no** constituye comparación automática completa de registros ni alimenta los estados CSV.

Entradas: objeto seleccionado con FullName y dos conexiones. Salidas: dos DataTable visibles y cambios locales aplicables al target. Fuentes: [DataCompareForm](../../../OpenDBDiff/UI/DataCompareForm.cs), [Updater.getData/CommitTable](../../../OpenDBDiff/Updater.cs), [entrada GUI](../../../OpenDBDiff/UI/MainForm.cs).

## Requisitos observados

| ID | Comportamiento |
|---|---|
| DATA-01 | Leer `SELECT * FROM <FullName>` en ambos lados con CommandBehavior.KeyInfo; cargar DataTable y mostrar grids de solo lectura |
| DATA-02 | Update row obtiene ItemArray de fila source y usa LoadDataRow en destino; contempla columnas Unique/AutoIncrement para claves nuevas |
| DATA-03 | Row to row copia valores por posición salvo columnas Unique de destino, sobre filas seleccionadas |
| DATA-04 | Merge combina DataTable con preserveChanges=true y marca como Added filas que permanecían Unchanged |
| DATA-05 | Las ediciones locales habilitan Commit Changes; al confirmar, GetChanges se envía a SqlDataAdapter/SqlCommandBuilder contra target; en éxito AcceptChanges y recarga |
| DATA-06 | Mostrar filas Added en verde y Modified en azul; errores de lectura/escritura se muestran mediante MessageBox |

## Limitaciones y decisiones pendientes

GAP-03: no se verificó con pruebas nuevas la corrección de Merge, claves compuestas, identidad, concurrencia, tipos incompatibles, alineación de columnas, filas nulas ni llamadas a Commit sin cambios. `SELECT *` no tiene paginación ni orden garantizado; leer dos grids no demuestra emparejamiento por clave. En error de lectura Updater devuelve el DataTable que haya obtenido, posiblemente vacío. No hay garantía de transacción global ni resolución de conflictos.

La especificación registra estos comportamientos, pero **no obliga a reproducir una inserción incorrecta ni a interpretar filas vacías como igualdad**. Antes de reconstruir, decidir si conservar esta capacidad, corregirla con un contrato explícito de claves/conflictos o separarla del producto. Antes de migrar, caracterizarla en laboratorio si forma parte de la entrega soportada.

## Aceptación pendiente de laboratorio

- **AC-DATA-01:** fixture con tabla y PK simple: leer ambos lados, elegir una fila y verificar los cambios locales antes de Commit; comprobar que no escribe antes de esa acción.
- **AC-DATA-02:** Row to row preserva columnas Unique; verificar columnas en distinto orden y rechazo o resultado documentado.
- **AC-DATA-03:** claves identity/compuestas, NULL, Unicode y tipos binarios: caracterizar Update row y Merge, registrar resultados antes de fijar contrato nuevo.
- **AC-DATA-04:** Commit de un cambio válido altera solo el target del laboratorio; falla de permisos o conflicto devuelve error y permite inspección.
- **AC-DATA-05:** tabla grande y fallo de lectura: registrar tiempo/memoria y experiencia sin inventar un SLA o paginación existente.

No se identificaron pruebas automatizadas específicas de DataCompareForm/CommitTable en la suite inspeccionada. Estos escenarios son pendientes, no resultados aprobados ni autorización para ejecutar DML.
