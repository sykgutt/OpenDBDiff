# SPEC: configuracion-conexiones

## Objetivo, entradas y salidas

Preparar dos conexiones y opciones, guardar proyectos y restaurar la última configuración. Entradas: conexión izquierda/derecha, tipo de proyecto, nombre, opciones y ajustes del comparador externo. Salidas: generadores configurados, estado de conexión/error y persistencia local.

Fuentes: [Project](../../../OpenDBDiff/Settings/Project.cs), [SettingsDatabase](../../../OpenDBDiff/Settings/SettingsDatabase.cs), [selector SQL](../../../OpenDBDiff.SqlServer.Ui/Front), [IFront](../../../OpenDBDiff.Abstractions.Ui/IFront.cs), [opciones](../../../OpenDBDiff.SqlServer.Schema/Options), [ExternalDiffSettings](../../../OpenDBDiff/Settings/ExternalDiffSettings.cs).

## Requisitos

| ID | Contrato observado |
|---|---|
| CFG-01 | Seleccionar conexiones izquierda/deseada y derecha/actual, autenticación Windows o SQL y servidor/base; probar conexión, listar bases accesibles y exponer nombre/error mediante IFront; SQLServer es el handler registrado |
| CFG-02 | Guardar, listar, renombrar, abrir, eliminar y crear nuevos proyectos; restaurar conexiones de la última configuración marcada IsLastConfiguration. Opciones activas son de sesión; no se acredita persistencia entre sesiones |
| CFG-03 | Persistir en `%LOCALAPPDATA%\OpenDBDiff\settings.liteDb`, colección projects; ajustes externos en externalDiff. No introducir un almacén paralelo |
| CFG-04 | Reconocer LiteDB normal y blob DPAPI CurrentUser; descifrar en memoria; conservar formato protegido al modificarlo y respaldo cifrado .bak por reemplazo atómico |
| CFG-05 | Un archivo DPAPI inválido o un fallo de reemplazo no debe sustituir el original por una base vacía. Una lectura sin cambios conserva bytes del archivo protegido |
| CFG-06 | Parsear el alias `Trust Server Certificate` como `TrustServerCertificate` sin reemplazar texto dentro de valores/contraseñas |
| CFG-07 | Conservar bloques Ignore, Filters, Comparison, Defaults y Script; el constructor SqlOption() usa SqlOptionIgnore(true). Valores y claves exactas en Inventario |
| CFG-08 | Persistir ruta ejecutable y plantillas para archivos/carpetas del comparador externo; valores vacíos recuperan plantillas predeterminadas |

## Reglas de opciones

Los flags `Filter*` no significan universalmente «ignorar cuando true»: muchos habilitan lectura/comparación. Conservar su semántica según consumidor. Por defecto `FilterFullTextPath=false`; el resto se deriva del constructor y se lista en [Inventario](Inventario.md).

Comparison: IgnoreWhiteSpacesInCode=false; CaseSensityInCode=CaseInsensity; CaseSensityType=Automatic y ReloadComparisonOnUpdate=false por valores iniciales. Defaults guarda expresiones para enteros, reales, texto, Unicode, fecha, binarios, GUID, tiempo y XML. Script inicia AlterObjectOnSchemaBinding=true.

Filters excluye por tipo/nombre o por esquema propietario. Comparación literal ordinal sin distinguir mayúsculas; comodines se convierten a regex anclada con IgnoreCase. La implementación de `?` es `.?` (cero o un carácter), y `*` es `.*`, con escapes `~`. No prometer glob estándar; caracterizar antes de sustituir el parser. La lista predeterminada incluye roles/esquemas de sistema y patrones históricos.

## Errores y seguridad

DPAPI depende del usuario Windows y sus claves. No implica portabilidad entre perfiles ni que las bases LiteDB nuevas se cifren automáticamente. La configuración contiene conexiones; logs y documentación deben evitar secretos. No se cambió su política de cifrado en esta tarea.

GAP-08: `Project.Options` lleva `[BsonIgnore]`, `SaveLastConfiguration` guarda conexiones y `form_OnSelect` restaura conexiones, no opciones. El método GetSerializedOptions no tiene consumidor en las rutas inspeccionadas. No prometer round-trip persistente de opciones por la mera existencia de esa propiedad. `Project.Upsert` puede devolver false y el host no comprueba ese resultado; caracterizar el error de guardado antes de rediseñarlo.

## Aceptación

- **AC-CFG-01:** guardar, renombrar, abrir y borrar proyecto sintético conserva las conexiones correspondientes; intercambio de selectores invierte ambas conexiones. Caracterizar por separado opciones de sesión y GAP-08, sin exigir persistencia inexistente.
- **AC-CFG-02:** abrir settings plano y DPAPI del usuario de prueba; lectura preserva bytes; escritura DPAPI preserva contenido y respaldo cifrado.
- **AC-CFG-03:** provocar fallo de reemplazo y entrada protegida inválida; original intacto y temporales limpios.
- **AC-CFG-04:** cadena con alias de certificado y valor con punto y coma conserva valores al normalizar.
- **AC-CFG-05:** caracterizar patrones literales, `*`, `?`, escapes y exclusión por esquema, incluyendo copia de opciones mediante sus constructores.

Pruebas: [SettingsDatabaseTests](../../../OpenDBDiff.Tests/Settings/SettingsDatabaseTests.cs), [SqlConnectionStringCompatibilityTests](../../../OpenDBDiff.Tests/Settings/SqlConnectionStringCompatibilityTests.cs). El CRUD completo de proyectos, los diálogos y filtros carecen aquí de certificación nueva.
