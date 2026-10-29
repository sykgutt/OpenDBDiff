# Decisión: migrar o reconstruir

Fecha: 2026-10-29. **Aceptado:** preparar 4.8.1 como etapa previa. **Abierto:** migración posterior a .NET 10.0.14 o reconstrucción funcional. No se ha autorizado una arquitectura nueva ni una reescritura.

## Comparación para decidir

| Dimensión | Retarget 4.8.1 | Migración .NET 10 | Reconstrucción desde especificación |
|---|---|---|---|
| Código y comportamiento | Conserva casi toda la implementación | Reutiliza motor y funciones compatibles; adapta plataforma/paquetes | Reimplementa contratos seleccionados y necesita oráculo de equivalencia |
| Proyectos | Clásicos/packages.config inicialmente | Planificar SDK-style, recursos, referencias y paquetes | Elegir estructura tras aprobar arquitectura |
| UI | WinForms existente | WinForms puede continuar en Windows; validar diseñador, DPI y Scintilla | Permite otro diseño; nueva tecnología aún no elegida |
| Configuración | LiteDB/DPAPI conservados | Probar APIs/paquetes Windows y portabilidad de archivos | Necesita lector/importador compatible o transición explícita |
| Pruebas | Conservar MSTest clásico si funciona | Adaptar framework/runner y mantener regresiones | Portar escenarios y crear corpus amplio de paridad |
| Riesgo dominante | Entorno, retargeting, dependencias antiguas | Dependencias nativas, APIs Framework, recursos y despliegue | Omitir comportamiento, invertir dirección o generar SQL incorrecto |
| Beneficio | Cambio acotado y baseline más reciente | Plataforma moderna con reutilización | Rediseño de límites y defectos, mayor coste de validación |

Las conclusiones sobre esfuerzo son inferencias de la estructura del repositorio, no resultados de un prototipo. Estimaciones previas orientativas: 4.8.1 **0.5–2 días**; migración .NET 10 **3–7 días** si dependencias son compatibles, **1–3 semanas o más** si deben sustituirse. La reconstrucción todavía no tiene una estimación defendible: requiere cerrar alcance y pruebas por familia de objeto. No usar estos rangos como promesa de entrega.

## Componentes candidatos a reutilización

- Motor de modelo/comparación/SQL: alto valor acumulado y muchas reglas por objeto. Reutilizar solo tras comprobar APIs, recursos y resultados; no asumir pureza de Compare ni clonación completa.
- CSV y helpers externos: contratos relativamente aislados, con pruebas. CSV no depende de WinForms, aunque vive en el proyecto GUI y se enlaza en Tests.
- SettingsDatabase: contrato claro de formato y protección, pero dependiente de Windows/DPAPI. Revisar paquetes equivalentes y archivos existentes antes de portar.
- Interfaz y adaptador: `IFront`/`Abstractions.Ui` exponen controles WinForms, MainForm mezcla orquestación/efectos, Updater mezcla SQL y MessageBox. No se puede reemplazar la UI suponiendo que todas las abstracciones son neutrales.
- ScintillaNET, System.Deployment, QualityTools y assets NuGet antiguos: investigar compatibilidad y uso real antes de decidir reemplazos. No se hizo una auditoría de vulnerabilidades de paquetes.

## Prueba de viabilidad posterior

Después de 4.8.1, realizar un prototipo aislado, conservando baseline funcional:

1. Compilar contratos/motor y ejecutar fixtures de columnas, memoria y generación en .NET 10.
2. Construir una ventana mínima con el editor y recursos nativos en arquitecturas acordadas.
3. Abrir y modificar copias sintéticas LiteDB/DPAPI bajo el mismo usuario; verificar compatibilidad con el formato anterior.
4. Exportar el mismo corpus CSV y generar SQL; comparar excluyendo únicamente cabeceras variables justificadas.
5. Probar CLI, publicación y arranque en equipo limpio; elegir política framework-dependent/self-contained según el entorno de entrega.
6. Registrar bloqueos concretos, alternativas y esfuerzo de remediación, sin extender el prototipo a una reescritura implícita.

La [descarga oficial .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) consultada el 2026-10-29 lista 10.0.14 como runtime/Desktop Runtime. El SDK tiene otra numeración y el target WinForms sería net10.0-windows. Revalidar parche y soporte al comenzar; no congelar una versión vulnerable solo por figurar aquí.

## Criterios de decisión y puertas

Preferir migración si el prototipo conserva los contratos críticos y los bloqueos tienen correcciones acotadas. Considerar reconstrucción si dependencias o acoplamientos impiden los objetivos aprobados y el coste medido de corregirlos supera reimplementar y certificar el alcance. La comparación debe incluir pruebas, transición de datos/configuración y mantenimiento, no solo líneas de código.

Antes de reconstruir: acordar versiones Windows/SQL, edición de datos, ejecución directa, opciones persistentes, cambios de UI, requisitos de rendimiento y resolución de [GAP-01..09](Especificación/Trazabilidad.md). El inventario de capacidades no basta para declarar reconstruible cada variante sin ampliar fixtures por familia.

Cerrar decisión con responsable, fecha, evidencia del prototipo, alternativa elegida, razones, contratos que cambian y plan de recuperación. Hasta entonces mantener ambas alternativas documentadas y no inventar prioridades de negocio.
