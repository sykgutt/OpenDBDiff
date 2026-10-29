# Continuidad para un nuevo chat

## Estado y decisiones

Revisión documental del **2026-10-29**, rama `OpenDiffBuru`, HEAD `4d3633876537599a29cb302070486682af3d1acf`. Árbol limpio antes de crear este paquete. Repetir `git status --short` y `git rev-parse HEAD` al retomar: esta instantánea no describe cambios posteriores.

- Objetivo inmediato: preparar la migración 4.5.2 → 4.8.1 conservando WinForms, CLI, proyectos clásicos y paquetes inicialmente.
- Objetivo posterior: evaluar .NET 10.0.14 frente a reconstrucción equivalente. No se ha elegido reconstruir.
- Se aprobó especificación por capacidades y equivalencia funcional; no una réplica visual exacta.
- No se autoriza por este documento instalar componentes, desplegar, aplicar SQL, hacer commit ni push.
- La nueva documentación es una baseline revisada en código, no una certificación exhaustiva de todas las variantes de SQL Server.

## Comprobaciones de esta sesión

| Evidencia | Resultado |
|---|---|
| Git, proyectos y paquetes | HEAD y rama anteriores; siete targets v4.5.2; packages.config |
| Configuración de hosts | GUI declara sku v4.5.2; CLI tiene appSettings y no bloque startup |
| CI | AppVeyor, imagen Visual Studio 2019, Release y artefactos GUI/CLI |
| Referencias 4.8.1 en ruta estándar | No existe `C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8.1/mscorlib.dll` |
| Registro local de runtime | `HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full`, Release = `534296`; runtime y targeting pack son comprobaciones distintas |
| Grafo Codebase Memory | No se expusieron herramientas MCP del grafo en esta sesión; sin confirmación de generación/cobertura |
| Evidencia de funciones | Lectura directa de hosts, modelo, comparación, lectores, CSV, settings, auxiliares y pruebas |
| Compilación, MSTest, GUI, SQL | No ejecutados durante este cambio documental |

El valor de registro supera el umbral mínimo 533320 de 4.8.1; **no significa que el proyecto ya apunte a 4.8.1**. Consultar el [método de Microsoft](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed). No se inspeccionaron credenciales ni archivos reales de configuración.

## Evidencia histórica, no repetida

El [seguimiento](../Seguimiento.md) registra build Debug, **72/72 MSTest** y probe CSV WinForms sintético correctos para el trabajo de octubre de 2026. El probe sustituye el diálogo nativo. El laboratorio de memoria anterior usó SQL Server 17.0.1145.4 y bases desechables; no acredita ejecución en SQL Server 2014/2016. Para repetir, seguir [Ejecución](../Ejecucion.md) y distinguir suite, probes y laboratorio.

## Hallazgos que el siguiente chat debe conservar

1. GUI: izquierda/source = deseada; derecha/target = actual. Motor: origin = actual y destination = deseada. El script transforma derecha hacia izquierda.
2. CSV: seis columnas, etiqueta `new item`, nombre `Diff<target>.csv`. Nombres capturados al comparar; `Database.Clone()` pierde `Name`. No arreglar Clone incidentalmente.
3. `changed item data` describe definición/estructura; los registros se manipulan en una función auxiliar separada.
4. Tablas/índices fallan con contexto ante errores; algunos lectores restantes conservan `MessageLog` sin abortar. FileGroups relanza su excepción. Ver GAP-01 en la especificación.
5. La GUI ejecuta operaciones por objeto mediante `Updater`; Update all no equivale a ejecutar íntegramente el script generado. No prometer sincronización total o transacción global.
6. Settings puede ser LiteDB normal o blob DPAPI CurrentUser. Conservar protección, reemplazo atómico y respaldo cifrado. No versionar ni mostrar las conexiones.
7. Tests clásicos requieren ensamblados de Visual Studio. No sustituirlos automáticamente por paquetes modernos para resolver un bloqueo del entorno.

## Preguntas pendientes y momento de resolverlas

| Pregunta | Necesaria antes de |
|---|---|
| ¿Qué versiones/ediciones de Windows usan realmente GUI y CLI? | Comprometer la compatibilidad de 4.8.1 y desplegar |
| ¿Qué versiones de SQL Server deben certificarse? | Definir laboratorios y declarar soporte probado |
| ¿Se autoriza instalar Developer Pack o componentes VS faltantes? | Modificar el equipo |
| ¿Se autoriza crear bases desechables para validar escrituras? | Ejecutar pruebas de aplicación SQL o de datos |
| ¿Se conservan edición de datos y Update all en una reconstrucción, o se rediseñan? | Aprobar alcance de la reconstrucción; hoy se documentan como existentes |
| ¿Se conserva WinForms y qué política de distribución/actualizaciones se usará en .NET 10? | Planificar la segunda migración |

Estas preguntas no impiden usar ni completar la documentación. No cambiar una decisión ya aprobada sin evidencia o nueva petición.

## Texto para iniciar otro chat

> Trabaja en D:\GIT REPOS\OpenDBDiff. Lee AGENTS.md, Documentacion/migración/README.md, Continuidad.md, Migracion-net481.md y tasks/todo.md. Este paquete parte del commit 4d36338 y fue revisado el 2026-10-29; verifica Git y fuentes actuales antes de reutilizarlo. El proyecto continúa en .NET Framework 4.5.2. Quiero continuar la preparación/migración a 4.8.1, como etapa previa a .NET 10.0.14; confirma conmigo qué etapa ejecutar si mi solicitud no lo especifica. La especificación busca equivalencia funcional y la decisión de reconstruir sigue abierta. Conserva los contratos CSV/source-target, restricciones de memoria y configuración DPAPI. Usa Codebase Memory si está disponible y registra cobertura o fallback. Distingue pruebas históricas de las nuevas. No apliques SQL sobre mis bases ni hagas commit/push por inferencia. Atiende las preguntas que bloqueen cada etapa y actualiza el seguimiento con evidencia.
