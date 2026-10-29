# SPEC: interfaz-cli

## Objetivo y dependencias

Orquestar los contratos del motor para usuarios de escritorio y automatización. GUI depende de todas las capacidades; CLI solo de lectura, comparación y generación. No requiere replicar píxeles en una reconstrucción, sí preservar operaciones, estados, mensajes útiles y orientación.

Fuentes: [MainForm](../../../OpenDBDiff/UI/MainForm.cs), [ProgressForm](../../../OpenDBDiff/UI/ProgressForm.cs), [SchemaTreeView](../../../OpenDBDiff/UI/SchemaTreeView.cs), [Program CLI](../../../OpenDBDiff.CLI/Program.cs), [CommandlineOptions](../../../OpenDBDiff.CLI/CommandlineOptions.cs), [IProjectHandler](../../../OpenDBDiff.Abstractions.Ui/IProjectHandler.cs).

## Flujos y requisitos

| ID | Contrato observado |
|---|---|
| HOST-01 | Al iniciar, registrar handler SQLServer, construir selectores y restaurar última configuración; ofrecer proyectos y opciones |
| HOST-02 | Compare captura nombres, invalida CSV anterior, lee actual/deseada y compara con ProgressForm(rightPair,leftPair). Si falla, mostrar contexto y no habilitar CSV nuevo |
| HOST-03 | Éxito: árbol con diferencias y copia actual; detalle old/new/diff mediante DiffPlex; script generado en Scintilla; guardar última configuración desde el flujo de comparación |
| HOST-04 | Árbol agrupa objetos, separa tablas normales/memoria, cuenta visibles/total y conserva selección al filtrar; selección determina generación SQL, no CSV |
| HOST-05 | Bloqueo SQL conserva árbol/resultados, vacía script y deshabilita copiar/guardar; exportar CSV válido permanece disponible |
| HOST-06 | Acciones explícitas: copiar/guardar SQL, comparar externamente seleccionado/todos, actualizar objeto/todos, abrir comparación de datos y administrar proyectos/opciones |
| HOST-07 | CLI requiere `-b/--before` y `-a/--after`; `-o/--outputfile` opcional. Usa SqlOption por defecto, sin flags de filtrado equivalentes a toda la GUI |
| HOST-08 | CLI prueba conexiones, lee, compara y llama ToSqlDiff dos veces como workaround; con -o escribe archivo, sin -o mezcla mensajes y SQL en consola |
| HOST-09 | CLI devuelve 0 si Work termina con éxito y 1 en caso contrario; mensajes de excepción/reportar issue en consola. Bajo depurador espera una tecla |

El enlace a ayuda/issues de CLI procede de App.config con fallback. No tratar stdout como protocolo de SQL puro ni afirmar códigos especiales de ayuda: ParseArguments y `completedSuccessfully` determinan la salida actual.

## Estado y errores

La comparación se realiza síncronamente desde Activated de ProgressForm. No hay contrato de cancelación cooperativa implementado a través de los lectores; cerrar un diálogo no acredita cancelar una consulta SQL en vuelo. Una reconstrucción podrá introducirlo con requisitos propios.

MainForm usa ErrorForm/manejo contextual; ProgressForm captura Error, ErrorLocation y progreso reciente. Los botones de actualización conservan las limitaciones de SYN/GAP-02. Cancelar SaveFileDialog CSV no crea archivo; cancelar confirmaciones de actualización debe caracterizarse sin conectar a bases del usuario.

GAP-09: el guardado SQL convencional comprueba FileName después de ShowDialog sin comprobar DialogResult. Cancelar con una ruta anterior podría reutilizarla. No extender a SQL la garantía de cancelación comprobada del exportador CSV; incorporar regresión antes de corregir ese comportamiento.

## Comandos

Desde raíz y después de compilar:

```powershell
& .\OpenDBDiff\bin\Debug\OpenDBDiff.exe
& .\OpenDBDiff.CLI\bin\Debug\OpenDBDiff.CLI.exe --help
& .\OpenDBDiff.CLI\bin\Debug\OpenDBDiff.CLI.exe -b 'Server=localhost;Database=BaseActual;Integrated Security=True;' -a 'Server=localhost;Database=BaseDeseada;Integrated Security=True;' -o '.\sincronizacion.sql'
```

Último comando requiere laboratorio autorizado; no aplica el archivo generado. Evitar credenciales en argumentos/logs cuando se use autenticación distinta de Windows.

## Aceptación

- **AC-HOST-01:** iniciar con configuración sintética válida y con error; no vaciar ni sobrescribir un archivo inválido.
- **AC-HOST-02:** comparación correcta, nueva comparación fallida y comparación con bloqueo SQL mantienen habilitaciones correspondientes, sin reutilizar CSV anterior.
- **AC-HOST-03:** filtros por estado, checkboxes y nodos contraídos conservan selección/contadores; verificar identidad entre objetos con IDs repetidos.
- **AC-HOST-04:** teclado, scroll del panel de botones, DPI y cancelación funcionan; Export CSV mantiene posición acordada.
- **AC-HOST-05:** CLI con entrada válida genera salida y 0; conexión inválida o script bloqueado devuelve 1; con -o no truncar destino antes de validar.
- **AC-HOST-06:** comparar SQL GUI/CLI con las mismas opciones y objetos; excluir solo metadatos variables de cabecera.

Evidencia: [MemoryGuiProbe](../../../OpenDBDiff.Tests/Integration/MemoryGuiProbe.cs), [CsvExportGuiProbe](../../../OpenDBDiff.Tests/Integration/CsvExportGuiProbe.cs), [MemoryLab](../../../OpenDBDiff.Tests/Integration/MemoryLab.cs). No existe aquí evidencia nueva de un recorrido manual completo de todos los formularios ni de todas las variantes CLI.
