# Plan de continuidad por etapas

## Etapa documental — realizada

Baseline → guía 4.8.1 → contratos de lectura/modelo → configuración/comparación → sincronización/reportes/datos → hosts → trazabilidad/alternativas → revisión de enlaces y comandos.

Entrega en [índice](../README.md). La aprobación del plan autorizó estos documentos, no implementación ni instalación. Ver [tareas](todo.md) para resultados y pendientes.

## Etapa de migración — pendiente

1. Revisar Git, SO de destino y herramientas. Resolver compatibilidad Windows e instalación faltante antes de retargetear.
2. Ejecutar baseline 4.5.2 y conservar artefactos/resultados. Registrar defectos existentes sin cambiar alcance.
3. Retargetear proyectos/hosts a 4.8.1 conservando dependencias y formato clásico; verificar resolución NuGet.
4. Compilar Debug/Release, ejecutar suite y probes sintéticos. Atender fallos sin retirar pruebas.
5. Completar GUI/CLI/entrega y laboratorio SQL solo si fue autorizado; resolver qué brechas bloquean la entrega.
6. Actualizar CI y documentación operativa con resultados reales. Entregar paquete recuperable.

## Etapa posterior — pendiente de decisión

Prototipo .NET 10 → medición de bloqueos → decisión migrar/reconstruir → especificaciones detalladas por familia y arquitectura aprobada → implementación incremental y paridad.

No avanzar una tarea que dependa de una respuesta pendiente; sí continuar documentación/revisión independiente. No es necesario reaprobar decisiones ya tomadas. Registrar cambios de alcance y evidencia por etapa.
