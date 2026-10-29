# Migración prevista a .NET Framework 4.8.1

Estado: **plan documentado, no ejecutado**. Baseline y entorno: [Continuidad](Continuidad.md), 2026-10-29. El cambio busca reducir el riesgo de plataforma manteniendo el comportamiento de la aplicación. No introduce interfaces públicas nuevas.

## Destino y compatibilidad

Se migra primero a **.NET Framework 4.8.1 para posteriormente migrar a .NET 10.0.14**, sujeto a la evaluación de [migrar o reconstruir](Decision-migrar-o-reconstruir.md). No es un prerrequisito técnico obligatorio para .NET moderno: es la secuencia elegida para separar riesgos.

Microsoft distingue ejecutar un binario antiguo sobre un runtime reciente de recompilarlo para un nuevo target. El segundo puede activar cambios de comportamiento. Hay que revisar los cambios acumulados desde 4.5.2, no solo los de 4.8.1. Fuentes: [compatibilidad](https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/application-compatibility), [cambios 4.6.x](https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/retargeting/4.6.x) y [4.8.x](https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/retargeting/4.8.x).

La matriz oficial permite 4.8.1 en Windows 11, determinadas versiones de Windows 10 (20H2 en adelante de las listadas), Windows Server 2022 y Server 2025. **Windows Server 2019/2016 y Windows 7/8.1 no admiten este destino**. Compatibilidad de instalación no equivale a soporte vigente del sistema operativo. Confirmar edición, versión y ciclo de soporte de cada equipo antes de entregar. Fuente: [requisitos oficiales](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements).

## Herramientas y dependencias

Para desarrollar: Visual Studio 2022 con desarrollo de escritorio .NET, MSBuild, NuGet, **Developer Pack/targeting pack 4.8.1** y componentes de pruebas clásicos. Para ejecutar: runtime 4.8.1 en Windows compatible. Tener `dotnet` instalado no basta. El runtime 4.5.2 está fuera de soporte; esto no permite inferir qué runtime usa cada usuario. [Instalación para desarrolladores](https://learn.microsoft.com/en-us/dotnet/framework/install/guide-for-developers) y [descargas 4.8.1](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481).

| Componente actual | Tratamiento inicial | Validación requerida |
|---|---|---|
| DiffPlex 1.6.3 | Conservar | Diff visual, texto Unicode y saltos |
| ScintillaNET 3.6.3 | Conservar wrapper y recursos nativos | Carga en arquitectura efectiva, edición, copia, márgenes, lexer y DPI |
| LiteDB 5.0.9 | Conservar formato y versión | Abrir/guardar datos sintéticos planos y DPAPI, respaldo y errores |
| Essy.Tools.InputBox 1.0.0 | Conservar | Diálogos y cancelación |
| CommandLineParser 2.8.0 | Conservar | Argumentos, ayuda, errores y códigos 0/1 |
| TSQL.Parser 1.5.2 | Conservar | Normalización y equivalencia de resultados |
| Moq 4.7.145, Castle.Core 4.4.1 | Conservar | Descubrimiento y ejecución de pruebas |
| ValueTuple 4.5.0, Tasks.Extensions 4.5.4, Unsafe 4.7.1 | Revisar resolución sin actualizar por rutina | Conflictos de referencias, redirects y copia a salida |
| MSTest clásico / QualityTools | Conservar inicialmente | Localizar ensamblado y runner; suite completa descubierta |
| System.Data.SqlClient, System.Security, Windows Forms | Referencias del Framework | Conexión, TLS/certificados, DPAPI y controles |

Esta tabla es inventario y estrategia, **no certificación de compatibilidad** de cada paquete. Si aparece un bloqueo, registrar error reproducible y proponer el cambio mínimo por separado.

## Cambios concretos previstos

1. Cambiar `TargetFrameworkVersion` a `v4.8.1` en los siete proyectos de la solución: GUI, CLI, Abstractions.Schema, Abstractions.Ui, SqlServer.Schema, SqlServer.Ui y Tests. Mantener formato clásico, referencias entre proyectos y archivos enlazados.
2. GUI `App.config`: cambiar el `sku` a `.NETFramework,Version=v4.8.1`, manteniendo `supportedRuntime version="v4.0"` (CLR). CLI: añadir el mismo bloque `startup`, conservando `appSettings`.
3. Revisar `packages.config` y HintPath después del retarget. No hacer una sustitución ciega de `net452` en rutas: una DLL net45/net40 compatible puede seguir siendo el asset correcto. Si NuGet pide reinstalar, hacerlo con las mismas versiones, revisar el diff y documentar la resolución. No migrar a PackageReference/SDK-style en esta etapa.
4. Retirar del comando de build el override de referencias **net452**; usar el pack 4.8.1 instalado. Mantener el workaround de `ReferencePath` para QualityTools solo donde se necesite. Inspeccionar conflictos y redirects concretos; no añadir switches de compatibilidad sin regresión reproducida.
5. Actualizar AppVeyor a una imagen disponible con VS 2022 y runtime/pack 4.8.1, verificándolos en el agente. Conservar Release y artefactos GUI/CLI; hacer explícita la ejecución y resultado de pruebas. No asumir que cambiar el nombre de imagen instala todos los componentes clásicos.
6. Actualizar ejecución, AGENTS y seguimiento **después** de confirmar el nuevo target. Mantener este documento como historial de decisión y anotar el commit migrado.

## Comandos y secuencia

Desde la raíz, primero registrar el estado y localizar herramientas. Bloque de inspección, sin instalación:

```powershell
git status --short
git rev-parse HEAD
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Falta vswhere/Visual Studio.' }
$vsRoot = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath | Select-Object -First 1
if (-not $vsRoot) { throw 'No se encontró Visual Studio con MSBuild.' }
$migrationMsbuild = Join-Path $vsRoot 'MSBuild\Current\Bin\MSBuild.exe'
$migrationVstest = Join-Path $vsRoot 'Common7\IDE\Extensions\TestPlatform\vstest.console.exe'
$migrationReferences = Join-Path $vsRoot 'Common7\IDE\PublicAssemblies'
Test-Path (Join-Path $migrationReferences 'Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll')
Test-Path "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1\mscorlib.dll"
Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release | Select-Object Release
Get-Command nuget -ErrorAction Stop
```

Ejecutar antes la baseline 4.5.2 con los comandos de [Ejecución](../Ejecucion.md), conservando logs y conteo de pruebas. No usar esos overrides net452 para certificar 4.8.1. Crear una rama `codex/migracion-net481` al implementar, partiendo de la baseline acordada y preservando cambios ajenos.

Después de instalar requisitos autorizados y efectuar el retarget, ejecutar este bloque en la misma sesión PowerShell del anterior:

```powershell
nuget restore .\OpenDBDiff.sln
if ($LASTEXITCODE -ne 0) { throw 'Falló restore.' }
foreach ($migrationConfiguration in @('Debug', 'Release')) {
    & $migrationMsbuild .\OpenDBDiff.sln /t:Rebuild /m "/p:Configuration=$migrationConfiguration" '/p:Platform=Any CPU' "/p:ReferencePath=$migrationReferences" /p:ResolveNuGetPackages=false /verbosity:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Falló build $migrationConfiguration." }
    & $migrationVstest ".\OpenDBDiff.Tests\bin\$migrationConfiguration\OpenDBDiff.Tests.dll" /Logger:trx
    if ($LASTEXITCODE -ne 0) { throw "Fallaron pruebas $migrationConfiguration." }
}
```

Estos comandos son propuestas verificadas sintácticamente, no ejecutadas contra un proyecto retargeteado. Si no se descubre la suite clásica completa, detener la aceptación y resolver runner/referencias; no aceptar cero pruebas como éxito.

Arranque posterior y ejemplo CLI (usar únicamente bases de laboratorio autorizadas):

```powershell
& .\OpenDBDiff\bin\Debug\OpenDBDiff.exe
& .\OpenDBDiff.CLI\bin\Debug\OpenDBDiff.CLI.exe -b 'Server=localhost;Database=BaseActual;Integrated Security=True;' -a 'Server=localhost;Database=BaseDeseada;Integrated Security=True;' -o '.\sincronizacion.sql'
```

La CLI genera un archivo; no lo aplica. Los botones de actualización y Commit Changes de la GUI sí pueden escribir en SQL Server.

## Aceptación de la migración

| Puerta | Evidencia para cerrar |
|---|---|
| Plataforma | Windows de destino acordados; pack/runtime identificados; salidas con TargetFrameworkAttribute 4.8.1 |
| Build y pruebas | Siete proyectos Debug/Release; suite completa, sin tests retirados o ignorados para aprobar |
| Contratos del motor | Mismos estados/selección/SQL sobre fixtures iguales; normalizar solo fecha, máquina y usuario de la cabecera al comparar resultados |
| CSV | Seis columnas, `new item`, acciones, nombres capturados, BOM/CRLF y fallo sin perder destino |
| Persistencia | Datos sintéticos planos/DPAPI, mismo usuario, lectura sin modificación y respaldo cifrado; entrada inválida preservada |
| GUI | Inicio, selectores/opciones/proyectos, árbol, filtros, Scintilla, DPI 100/150/200%, teclado, cancelación de diálogos y errores |
| Comparador externo | Configurar, cancelar y abrir seleccionado/todos; old/current a la izquierda externa |
| CLI | Ayuda, argumentos inválidos, generación correcta, exit 0/1, archivo intacto ante bloqueo previo a guardado |
| SQL y datos auxiliares | Laboratorio separado autorizado: operaciones soportadas y rechazos; no confundir tests sintéticos con prueba real |
| Entrega | Agente CI reproducible, carpetas completas con DLL/config/nativos y prueba en máquina compatible limpia |

Usar la [trazabilidad](Especificación/Trazabilidad.md) para seleccionar regresiones. Las brechas preexistentes se registran y se evalúa si bloquean la entrega; no deben esconderse ni ampliarse durante el retarget.

## Recuperación

Guardar baseline de código y carpetas de distribución completas. Probar restauración de settings sobre copias protegidas del mismo usuario; no introducir backups con secretos en Git. Si falla la nueva aplicación, volver al paquete anterior validado y a la configuración compatible preservada. No usar `git reset --hard` ni borrar cambios ajenos.

Las versiones 4.x se actualizan **in-place**: volver al ejecutable anterior no revierte el runtime. Para comparar runtimes o recuperar el sistema usar una VM/imagen previamente preparada; no prometer desinstalar un Framework incluido en Windows. [Requisitos y actualización in-place](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements).

## Complejidad y siguiente etapa

Estimación inicial orientativa para el retarget: **0.5–2 días técnicos** si entorno/dependencias funcionan; pruebas GUI, equipos de destino y CI pueden ampliar el plazo. No es un compromiso ni una medición.

La consulta oficial del 2026-10-29 mostró **runtime/Desktop Runtime 10.0.14** y SDK **10.0.403** y **10.0.114**. Son numeraciones distintas. Para una futura GUI WinForms el TFM sería `net10.0-windows`, no `net10.0.14`; verificar de nuevo SDK, parche soportado y distribución antes de implementarla. [Descargas oficiales .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
