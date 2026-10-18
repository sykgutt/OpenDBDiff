# Documentación de OpenDBDiff

OpenDBDiff es una herramienta de comparación de esquemas para **Microsoft SQL Server 2005+**. Lee el catálogo de dos bases de datos, calcula las diferencias y genera un script T-SQL de sincronización.

Esta carpeta documenta la arquitectura, el flujo de ejecución, las tecnologías y el diseño interno del proyecto en la rama **OpenDiffBuru**.

## Contenido

| Documento | Descripción |
|---|---|
| [TablasEnMemoria.md](TablasEnMemoria.md) | Tablas en memoria, sección Schema, generación SQL y bloqueos |
| [Ejecucion.md](Ejecucion.md) | Requisitos, restauración, compilación, GUI, CLI, pruebas y diagnóstico |
| [Seguimiento.md](Seguimiento.md) | Estado del checkout, validaciones realizadas y pendientes |
| [CodebaseMemory.md](CodebaseMemory.md) | Integración MCP, consultas, cobertura y preparación de otra máquina |
| [../AGENTS.md](../AGENTS.md) | Reglas y contexto para continuar el trabajo en siguientes sesiones |
| [Arquitectura.md](Arquitectura.md) | Visión de arquitectura: capas, proyectos, dependencias, extensibilidad y decisiones de diseño |
| [DocumentoTecnico.md](DocumentoTecnico.md) | Documento técnico: stack, modelos, comparación, generación SQL, UI, CLI, opciones, pruebas y CI |
| [diagramas/01-arquitectura.md](diagramas/01-arquitectura.md) | Diagrama Mermaid de arquitectura de componentes |
| [diagramas/02-secuencia.md](diagramas/02-secuencia.md) | Diagrama Mermaid de secuencia (comparación y script) |
| [diagramas/03-flujo.md](diagramas/03-flujo.md) | Diagramas Mermaid de flujo de la GUI, del motor y de la CLI |

## Lectura recomendada

1. Ejecución (preparar el entorno y arrancar).
2. Seguimiento (estado y límites de lo verificado).
3. Arquitectura y diagramas (contexto y capas).
4. Documento técnico (detalle de implementación).

## Datos rápidos

- **Versión:** 0.13.0
- **Runtime:** .NET Framework 4.5.2
- **Licencia:** GNU GPL v2 (`LICENSE`)
- **Solución:** `OpenDBDiff.sln` (7 proyectos)
- **Acceso a SQL Server:** ADO.NET (`System.Data.SqlClient`), no SMO
- **Hosts:** WinForms (`OpenDBDiff.exe`) y consola (`OpenDBDiff.CLI.exe`)
