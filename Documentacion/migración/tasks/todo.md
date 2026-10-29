# Tareas y criterios de cierre

Actualización: 2026-10-29. D = documentación; M = migración; F = futuro .NET 10/reconstrucción. Los checks D no implican que las pruebas de producto hayan sido ejecutadas.

## Documentación

- [x] **D-01 Baseline y continuidad.** Aceptación: Git, target, dependencias, entorno y mensaje de nuevo chat. Verificación: fuentes/configuración inspeccionadas y Continuidad.md.
- [x] **D-02 Guía 4.8.1.** Aceptación: fuentes oficiales, comandos, cambios, riesgos, aceptación y recuperación. Verificación: enlaces Microsoft consultados y comandos revisados.
- [x] **D-03 Especificaciones.** Aceptación: mapa y siete capacidades, incluyendo datos auxiliares; contratos observados separados de brechas. Verificación: inspección de fuentes y referencias.
- [x] **D-04 Trazabilidad y alternativas.** Aceptación: cada requisito asignado a fuente/prueba o pendiente, decisión futura abierta. Verificación: matriz e inventario estático.
- [x] **D-05 Cierre documental.** Aceptación: enlaces, PowerShell y formato comprobados; índice, AGENTS y seguimiento actualizados. Evidencia final en Seguimiento.md.

## Implementación 4.8.1 — no realizada

- [ ] **M-01 Confirmar destino y preparar entorno.** Aceptación: Windows objetivo compatible, VS/pack/runtime/QualityTools disponibles. Verificar con comandos de inspección y resolver autorización de instalación si procede. Sin cambios en proyectos todavía.
- [ ] **M-02 Obtener baseline ejecutable.** Aceptación: build y suite actuales, fixtures y distribución anterior preservados. Verificar con Ejecucion.md; anotar conteo esperado/descubierto y logs. No usar registros históricos como sustituto.
- [ ] **M-03 Retargetear bibliotecas.** Aceptación: cuatro bibliotecas en v4.8.1, dependencias conservadas. Verificar build de bibliotecas y diff de sus cuatro csproj; no eliminar soporte funcional.
- [ ] **M-04 Retargetear hosts y pruebas.** Aceptación: GUI/CLI/Tests y startup coherentes. Verificar tres csproj y dos App.config; build completo y suite Debug/Release con resolución correcta de referencias. Revisar packages.config solo si restauración/retarget lo exige.
- [ ] **M-05 Verificar experiencias y contratos.** Aceptación: escenarios de Migracion-net481 y matriz cumplidos o limitaciones aceptadas. Verificar probes, diálogos nativos, CSV, CLI y settings sintéticos; anexar resultados por requisito.
- [ ] **M-06 Verificar SQL/datos en laboratorio autorizado.** Aceptación: versiones acordadas, casos soportados y bloqueados, limpieza comprobada. Registrar script/resultado sin secretos; si no se autoriza, dejar pendiente y limitar certificación.
- [ ] **M-07 CI y distribución.** Aceptación: agente con requisitos, Release y tests, artefactos completos, arranque en equipo compatible limpio. Verificar appveyor.yml y resultados del servicio; una inspección YAML no acredita CI ejecutada.
- [ ] **M-08 Actualizar continuidad.** Aceptación: AGENTS, Ejecucion, README, Seguimiento y paquete de migración reflejan target/commit y evidencia reales. Verificar enlaces y que los pendientes no estén marcados como terminados. Commit/push solo si se solicitan.

## Segunda etapa — no realizada

- [ ] **F-01 Verificar runtime/SDK .NET 10 y realizar prototipo.** Aceptación: evidencias de motor, editor, configuración, CLI y publicación; ninguna sustitución supuesta compatible sin prueba.
- [ ] **F-02 Resolver GAP-01..09 y alcance de reconstrucción.** Aceptación: cada brecha con decisión explícita (corregir, limitar, retirar con aprobación o caracterizar); ampliar corpus por familias de objetos y versiones.
- [ ] **F-03 Elegir migración o reconstrucción.** Aceptación: decisión razonada con coste medido, contratos y pruebas de paridad; arquitectura y tareas aprobadas antes de implementar.

Para cada tarea completada registrar fecha, commit, archivos, comandos/resultados, limitaciones y siguiente paso. Los nombres de tareas no obligan a modificar archivos que no necesiten cambios.
