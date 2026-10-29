# Migración y especificación de OpenDBDiff

Fecha de revisión: **2026-10-29**. Baseline: `4d3633876537599a29cb302070486682af3d1acf`, rama `OpenDiffBuru`, aplicación 0.13.0. El checkout estaba limpio al comenzar esta documentación.

**Estado: documentación preparada; migración NO ejecutada.** Los siete proyectos siguen en .NET Framework 4.5.2. No se instalaron herramientas, ejecutaron cambios SQL, hicieron commits ni push.

La decisión acordada es preparar primero **.NET Framework 4.8.1**, como etapa previa a una futura migración a **.NET 10.0.14**. La alternativa de reconstruir sigue abierta. La especificación busca equivalencia funcional y permite otra organización interna y visual; los defectos actuales no son requisitos que deban reproducirse.

## Orden de lectura

1. [Continuidad](Continuidad.md): punto de entrada para otro chat, evidencia y preguntas.
2. [Migración a 4.8.1](Migracion-net481.md): requisitos, cambios previstos, comandos y aceptación.
3. [Especificación](Especificación/README.md): contratos actuales, capacidades y escenarios.
4. [Trazabilidad](Especificación/Trazabilidad.md): requisitos, fuentes, pruebas y vacíos.
5. [Migrar o reconstruir](Decision-migrar-o-reconstruir.md): decisión posterior y criterios.
6. [Plan](tasks/plan.md) y [tareas](tasks/todo.md): pasos y estado de avance.

## Cómo mantener este paquete

- Al cambiar el código, actualizar el requisito y su evidencia, indicando el nuevo commit y la fecha.
- Una referencia al código es inspección estática; un nombre de prueba no acredita que haya pasado en esta sesión.
- Los resultados históricos proceden de [Seguimiento](../Seguimiento.md); no se extienden a otros motores, equipos ni runtimes.
- Resolver preguntas por etapa. Continuar tareas independientes; detener solo el trabajo que dependa de una decisión pendiente.
- Antes de ejecutar una migración, revalidar sistema operativo, herramientas y la versión objetivo. Esta documentación no autoriza aplicar SQL a bases del usuario.

## Documentación complementaria

[Ejecución actual](../Ejecucion.md), [arquitectura](../Arquitectura.md), [documento técnico](../DocumentoTecnico.md), [tablas en memoria](../TablasEnMemoria.md), [CSV](../ExportacionCsv.md), [reglas del repositorio](../../AGENTS.md).
