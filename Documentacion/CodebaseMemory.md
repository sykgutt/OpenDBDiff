# Codebase Memory

## Integración del proyecto

Codebase Memory es una herramienta de desarrollo para consultar un grafo del código; no es una dependencia NuGet ni se distribuye con OpenDBDiff.

En la sesión de documentación del 2026-10-17 se confirmó el servidor MCP conectado y el proyecto existente:

- Proyecto: `D-GIT-REPOS-OpenDBDiff`.
- Raíz: `D:/GIT REPOS/OpenDBDiff`.
- Estado inicial consultado: `ready`, 3327 nodos y 14002 relaciones. El refresco posterior en modo `full` devolvió `indexed`, 3355 nodos y 14030 relaciones.
- Paquete npm local detectado: `codebase-memory-mcp` 0.12.1.

Los conteos son una fotografía, no una condición de aceptación. Verificar la raíz y el estado al retomar el trabajo. En otra ubicación el nombre derivado puede cambiar.

## Uso mediante MCP

Estas son llamadas de herramientas MCP, no comandos PowerShell:

```text
list_projects({})
index_status({"project":"D-GIT-REPOS-OpenDBDiff"})
search_graph({"project":"D-GIT-REPOS-OpenDBDiff","name_pattern":"^Main$"})
```

Con el `qualified_name` exacto devuelto por la búsqueda:

```text
get_code_snippet({"project":"D-GIT-REPOS-OpenDBDiff","qualified_name":"<nombre exacto>"})
trace_path({"project":"D-GIT-REPOS-OpenDBDiff","function_name":"<nombre exacto>","direction":"both","depth":2})
check_index_coverage({"project":"D-GIT-REPOS-OpenDBDiff","paths":["OpenDBDiff.CLI/Program.cs"]})
```

Si el checkout todavía no está indexado, usar:

```text
index_repository({"repo_path":"<ruta absoluta del checkout>","mode":"full"})
```

No crear otro proyecto para la misma raíz si ya existe. Los proyectos observados se refrescan en segundo plano; reservar una reindexación manual para falta de índice o una actualización grande que requiera frescura inmediata.

## Cobertura y límites

Preferir el grafo para símbolos, dependencias y llamadas. Para literales, configuraciones y documentos, usar lectura directa o `rg`. Paginar cuando aparezca `has_more` o un cursor `next`.

Antes de sustentar una conclusión, comprobar todos los archivos de evidencia con `check_index_coverage`. `no_recorded_issue` no demuestra cobertura completa. En esta sesión hubo `freshness=metadata_changed` en archivos de código y `not_tracked` en archivos de proyecto; se contrastaron los puntos de entrada y la configuración con el contenido del disco.

El último `index_status` consultado registraba 14 archivos SQL parcialmente parseados: consultas del catálogo y fixtures de triggers. La respuesta inmediata de `index_repository` había reportado cero archivos parciales; para tomar decisiones se debe consultar siempre `index_status` y la cobertura de cada ruta, y leer los rangos vigentes antes de formular conclusiones sobre ellos. `bin`, `obj`, paquetes y otros archivos ignorados quedan fuera por diseño. La ausencia de incidencias reportadas no demuestra cobertura completa del código SQL.

El wrapper global tenía incompleta su caché del ejecutable. Se reparó mediante su descarga automática con verificación de checksum y `codebase-memory-mcp --version` confirmó 0.12.1. Se reutilizó la conexión MCP existente; no fue necesario ejecutar `install` sobre los demás clientes del equipo.

## Preparar otra máquina

## Corrección de conexión del 2026-10-17

La instalación administrada usada por Codex y el paquete npm son instalaciones distintas. Se verificó mediante un arranque MCP nuevo que la primera ejecuta **0.10.6**, mientras `codebase-memory-mcp.cmd` en PATH ejecuta **0.12.1**. Intentar iniciar esta última con la caché ocupada por el daemon 0.10.6 devuelve `conflicting CBM process is active`; comprobar solo `--version` del wrapper no valida la conexión MCP.

El proyecto incluye [../.codex/config.toml](../.codex/config.toml) y [../.codex/start-codebase-memory.cmd](../.codex/start-codebase-memory.cmd). El lanzador resuelve `%LOCALAPPDATA%\Programs\codebase-memory-mcp\codebase-memory-mcp.exe`, la instalación administrada de este equipo, y falla explícitamente si falta. Evita seleccionar accidentalmente otra versión mediante PATH. Los tiempos de arranque y de herramientas son 120 segundos. No se modificó la configuración global ni se cerraron sesiones de otros clientes.

Codex admite configuración MCP de proyecto en `.codex/config.toml` para proyectos de confianza: [documentación oficial](https://developers.openai.com/codex/mcp). Abrir el cliente desde la raíz del repositorio y reiniciar su conexión MCP para cargar el cambio; si el cliente indica que omite la configuración local por confianza, completar su reconocimiento de confianza para este checkout.

Validación: TOML parseado, lanzador `--version` = 0.10.6, conexión STDIO independiente con `initialize` y `index_status` correctos, proyecto `D-GIT-REPOS-OpenDBDiff` en estado `ready`. También funcionaron búsqueda de `StartComparison`, snippet y cobertura por MCP, y se refrescó el índice existente en modo `full`. Persisten 14 archivos SQL parcialmente parseados; esto limita cobertura, no indica desconexión.

Para actualizar versiones, coordinar todos los clientes que comparten caché y comprobar el handshake después de la actualización. En otra máquina se requiere esa instalación administrada o adaptar explícitamente el lanzador a una única instalación; este archivo no instala paquetes ni descarga ejecutables.

### Instalación inicial

El README incluido en el paquete local documenta esta instalación:

```powershell
npm install -g codebase-memory-mcp
codebase-memory-mcp install
```

El wrapper instalado requiere Node.js 18 o superior. `install` configura los clientes de agentes detectados y puede modificar sus configuraciones de usuario; revisar su alcance antes de ejecutarlo en otra máquina. Reiniciar el cliente y verificar que exponga `list_projects` e `index_repository`. Registrar el checkout mediante MCP como se muestra arriba.

No hace falta repetir una instalación global para cada repositorio. Tampoco se necesita agregar secretos ni un `package.json` de Node al proyecto C# para consultar el grafo. Las reglas persistentes para este repositorio están en [AGENTS.md](../AGENTS.md).
