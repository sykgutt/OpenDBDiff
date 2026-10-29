# Especificación funcional de la aplicación actual

Baseline documental: **2026-10-29 / 4d36338**. Audiencia: responsable técnico y equipo que migrará o reconstruirá OpenDBDiff. Objetivo: comparar esquemas SQL Server, revisar diferencias, generar sincronización y conservar las funciones auxiliares identificadas. La equivalencia es funcional; no exige reproducir disposición visual ni clases internas.

## Método Spec-Driven Development

1. **Especificar:** partir del [mapa aprobado](Mapa-capacidades.md); describir contratos observados con identificadores estables.
2. **Planificar:** seleccionar migración o reconstrucción, resolver preguntas y establecer pruebas de equivalencia.
3. **Desglosar:** usar [tareas](../tasks/todo.md) con evidencia de cierre.
4. **Implementar y verificar:** cambiar una capacidad por vez, sin rebajar requisitos para acomodar la implementación.

El usuario aprobó el mapa y la equivalencia funcional. El contenido detallado aquí es una baseline derivada del código, pendiente de aceptación de producto antes de reconstruir. No es una auditoría exhaustiva de todas las combinaciones de objetos/versiones.

## Convenciones y contratos comunes

| Término | Definición |
|---|---|
| Source / izquierda GUI | Base deseada o referencia |
| Target / derecha GUI | Base actual que recibiría cambios |
| Origin del motor / CLI before | Estado actual |
| Destination del motor / CLI after | Estado deseado |
| Old / izquierda del comparador externo | Definición actual |
| New / derecha del comparador externo | Definición deseada |
| Comparación completa | Todos los objetos cargados según opciones/permisos; no todos los objetos físicamente existentes sin excepción |
| Selección | Objetos marcados para generar SQL; independiente del alcance CSV |

Requisitos `CFG`, `MOD`, `CMP`, `SYN`, `REP`, `HOST`, `DATA` son contratos observados que se desea conservar. `GAP` identifica defectos, riesgos o ausencia de validación: no se exige reproducirlos. Los escenarios `AC-*` son criterios de aceptación; salvo indicación explícita no son tests automatizados existentes.

## Documentos

- [Configuración y conexiones](SPEC-configuracion-conexiones.md)
- [Lectura y modelo](SPEC-lectura-modelo.md)
- [Comparación](SPEC-comparacion.md)
- [Sincronización](SPEC-sincronizacion.md)
- [Reportes y comparación externa](SPEC-reportes-externo.md)
- [Interfaz y CLI](SPEC-interfaz-cli.md)
- [Datos auxiliares](SPEC-datos-auxiliares.md)
- [Inventario de contratos](Inventario.md): proyectos, dependencias, opciones, grupos y pruebas detectados en fuentes
- [Trazabilidad y brechas](Trazabilidad.md)

## Stack, estructura, comandos y estilo

Windows, C#, Framework 4.5.2, WinForms, ADO.NET SqlClient, NuGet clásico y MSTest/QualityTools. [Inventario](Inventario.md) y [guía de ejecución](../../Ejecucion.md) contienen rutas y versiones. GUI y CLI consumen el motor SQL Server; `Abstractions.Ui` también contiene tipos WinForms, por lo que no es un contrato independiente de plataforma.

Comandos normales desde Developer PowerShell en la raíz, con referencias 4.5.2 y MSTest disponibles:

```powershell
nuget restore .\OpenDBDiff.sln
msbuild .\OpenDBDiff.sln /m /p:Configuration=Debug '/p:Platform=Any CPU'
vstest.console.exe .\OpenDBDiff.Tests\bin\Debug\OpenDBDiff.Tests.dll /Logger:trx
& .\OpenDBDiff\bin\Debug\OpenDBDiff.exe
```

Si faltan referencias net452, usar el comando local completo de Ejecucion.md; no retargetear para resolver el entorno. Para el futuro 4.8.1 usar [su guía](../Migracion-net481.md).

Seguir `.editorconfig`: CRLF, salto final, sin espacios finales; C# con cuatro espacios. Clases/propiedades públicas PascalCase; respetar nombres heredados al migrar. Ejemplo real de `DiffCsvReport.cs`:

```csharp
public ReadOnlyCollection<DiffCsvRow> Rows { get; private set; }
public string SuggestedFileName { get; private set; }
```

## Estrategia de pruebas y límites

Pruebas unitarias: estados, SQL sintético, selección, protección de settings y CSV. Probes WinForms: controles reales, con límites explícitos del diálogo sustituido. Laboratorio SQL: solo con autorización, datos desechables, versión anotada y limpieza verificada. Pruebas manuales: diálogos nativos, DPI, conexión, herramienta externa y errores. No se fija un porcentaje de cobertura sin medición; la matriz debe identificar qué contratos faltan por cubrir.

- Siempre: preservar contratos, cambios ajenos y secretos; enlazar evidencia y diferenciar ejecución de inspección.
- Consultar cuando haga falta: SO/versiones SQL que debe soportar la entrega, cambios de alcance o escrituras de laboratorio no autorizadas.
- Nunca por inferencia: aplicar SQL en bases del usuario, publicar conexiones, quitar pruebas para aprobar o convertir un defecto conocido en requisito de una reconstrucción.

Éxito de la especificación: cada capacidad tiene entradas, salidas, reglas, errores, escenarios y trazabilidad; las lagunas son visibles. Éxito futuro de la migración/reconstrucción: cumplir esos contratos con evidencia nueva y decisiones explícitas sobre cada GAP.
