# Compilación y ejecución

Guía para el código de este repositorio. Plataforma: Windows, C#, Windows Forms y .NET Framework 4.5.2. La solución contiene siete proyectos clásicos; usa `packages.config`, no proyectos SDK de .NET moderno.

## Requisitos

- Visual Studio con desarrollo de escritorio .NET y MSBuild. El CI existente usa Visual Studio 2019; para instalaciones más recientes, comprobar las referencias antiguas como se indica abajo.
- Referencias de compilación de .NET Framework 4.5.2 y un runtime .NET Framework compatible para ejecutar los binarios. Tener instalado solamente el SDK `dotnet` no satisface todos los requisitos.
- NuGet CLI en `PATH`, o restauración desde Visual Studio.
- Para las pruebas: MSTest clásico, incluido `Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll`, y `vstest.console.exe`. El proyecto de pruebas no utiliza MSTest.TestFramework de NuGet.
- Para comparar: dos bases SQL Server accesibles y credenciales con visibilidad de los objetos que se desean comparar. Un catálogo parcialmente visible puede producir un diff incompleto.
- Opcional: WinMerge o Beyond Compare para el comparador externo de la GUI.

## Compilar desde Visual Studio

1. Abrir `OpenDBDiff.sln`.
2. Restaurar los paquetes NuGet de la solución.
3. Seleccionar `Debug | Any CPU`.
4. Establecer **OpenDBDiff** como proyecto de inicio.
5. Compilar y ejecutar con F5 o Ctrl+F5.

Para ejecutar la consola desde el IDE, establecer `OpenDBDiff.CLI` como proyecto de inicio y configurar sus argumentos. Bajo el depurador, la CLI espera una tecla antes de terminar.

## Compilar desde PowerShell

Desde la raíz, en Developer PowerShell de Visual Studio:

```powershell
nuget restore .\OpenDBDiff.sln
msbuild .\OpenDBDiff.sln /m /p:Configuration=Debug '/p:Platform=Any CPU'
```

Para una entrega, sustituir `Debug` por `Release`. Distribuir la carpeta de salida completa, con sus DLL y archivos `.config`, no solamente el `.exe`.

Si MSBuild no está en `PATH`, localizarlo por su componente, evitando seleccionar por accidente una instalación de SSMS:

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio.' }
& $msbuild .\OpenDBDiff.sln /m /p:Configuration=Debug '/p:Platform=Any CPU'
```

## Ejecutar la interfaz gráfica

```powershell
& .\OpenDBDiff\bin\Debug\OpenDBDiff.exe
```

1. Elegir servidor, autenticación y base en los dos selectores.
2. Colocar el **esquema deseado en el selector izquierdo** y el **esquema actual en el derecho**. El script transforma **derecha → izquierda**, según `MainForm.StartComparison`.
3. Revisar las opciones de comparación y pulsar **Compare**.
4. Seleccionar los objetos del árbol y revisar sus diferencias.
5. Abrir **Synchronized script**, revisar el T-SQL y guardarlo o copiarlo.

La selección del árbol afecta a la generación del script. Revisar su base `USE`, las operaciones DROP/ALTER y la posible reconstrucción de tablas antes de ejecutarlo con una herramienta SQL. La generación no equivale a haber aplicado los cambios.

La GUI también contiene acciones de actualización de objetos mediante `Updater`, que ejecutan SQL sobre la conexión derecha. Usarlas aplica cambios; no son parte del recorrido de lectura y generación descrito arriba.

**Compare data** es una función auxiliar para una tabla existente en ambas bases; no es una sincronización general de datos.

### Comparador externo

Después de comparar, usar **External diff** para el objeto seleccionado. Su menú contextual permite **Compare all differences** y **Configure external tool...**.

En la configuración, elegir el ejecutable instalado o un preset WinMerge/Beyond Compare. Los argumentos admiten `{left}`, `{right}`, `{lefttitle}` y `{righttitle}`. En la ventana de la herramienta externa, **izquierda = actual/old** y **derecha = deseado/new**; esta orientación es distinta a la de los selectores de bases en la ventana principal. El botón **Test** comprueba el lanzamiento del ejecutable, no una comparación completa.

Los archivos exportados se crean bajo `%TEMP%\OpenDBDiff\extdiff`. El código limpia sesiones anteriores al lanzar una nueva comparación.

## Ejecutar la CLI

Ejemplo con autenticación integrada y nombres de bases de laboratorio:

```powershell
& .\OpenDBDiff.CLI\bin\Debug\OpenDBDiff.CLI.exe `
    --before 'Server=localhost;Database=BaseActual;Integrated Security=True;' `
    --after 'Server=localhost;Database=BaseDeseada;Integrated Security=True;' `
    --outputfile '.\sincronizacion.sql'
$LASTEXITCODE
```

| Opción | Alias | Significado |
|---|---|---|
| `--before` | `-b` | Obligatoria: base actual, sobre la que se aplicarían las acciones |
| `--after` | `-a` | Obligatoria: esquema deseado |
| `--outputfile` | `-o` | Archivo de salida; se sobrescribe si existe |

La carpeta del archivo de salida debe existir. Sin `-o`, el script y los mensajes de progreso se imprimen en consola; usar `-o` para obtener un archivo SQL separado. Exit code: `0` si termina correctamente, `1` si falla o no se procesan los argumentos. La CLI genera el script y no lo ejecuta. No ofrece los filtros configurables de la GUI: construye `new SqlOption()`.

Se puede comprobar el arranque sin conexión con `OpenDBDiff.CLI.exe --help`; en esta implementación también devuelve `1`, aunque muestre correctamente la ayuda, porque no ejecuta el trabajo de comparación.

No incluir contraseñas reales en ejemplos, capturas o archivos versionados. La GUI guarda las cadenas de conexión en `%LOCALAPPDATA%\OpenDBDiff\settings.liteDb`, junto con proyectos y configuración del comparador externo.

### Configuración existente protegida con Windows

El acceso compartido `SettingsDatabase` reconoce tanto LiteDB directo como un archivo completo protegido por Windows DPAPI (`CurrentUser`). En este último caso, descifra en memoria y mantiene el cifrado al guardar; realiza un reemplazo atómico y conserva la versión cifrada anterior en `settings.liteDb.bak`. Abrir el archivo para leer no lo reescribe. No hace falta borrarlo ni proporcionar una contraseña de LiteDB: se necesita el contexto de Windows que lo protegió.

Si aparece **This data file is encrypted and needs a password to open** al arrancar una compilación anterior, utilizar la compilación corregida. Ese mensaje se reprodujo al entregar directamente a LiteDB un archivo DPAPI. No se debe reiniciar la configuración como solución automática.

La restauración de conexiones admite el alias `Trust Server Certificate` guardado por otras variantes, normalizándolo a `TrustServerCertificate` al interpretar la cadena. Se conservan sus valores; no se modifica el archivo al restaurar la ventana.

## Pruebas

Tras una compilación correcta, desde Developer PowerShell:

```powershell
vstest.console.exe .\OpenDBDiff.Tests\bin\Debug\OpenDBDiff.Tests.dll
```

Para localizar el ejecutor si no está en `PATH`:

```powershell
$vstest = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'Common7\IDE\Extensions\TestPlatform\vstest.console.exe' | Select-Object -First 1
if (-not $vstest) { throw 'No se encontró el ejecutor de pruebas de Visual Studio.' }
& $vstest .\OpenDBDiff.Tests\bin\Debug\OpenDBDiff.Tests.dll
```

Las pruebas presentes cubren normalización T-SQL, columnas, conversión hexadecimal, helpers de comparación externa, configuración DPAPI/LiteDB y compatibilidad del alias de certificado. No sustituyen una comparación real contra SQL Server ni la verificación manual completa de WinForms.

## Diagnóstico de compilación

| Síntoma | Acción |
|---|---|
| `nuget` o `msbuild` no reconocido | Usar Developer PowerShell; localizar MSBuild con `vswhere`; instalar/proveer NuGet CLI |
| `MSB3644`, faltan referencias net452 | Proveer las referencias exactas de .NET Framework 4.5.2; no cambiar el target solamente para ocultar el fallo |
| Falta `QualityTools.UnitTestFramework` | Comprobar los componentes MSTest clásicos y la ruta de ensamblados públicos del IDE |
| NuGet menciona `TargetFrameworks` en estos proyectos clásicos | Comprobar archivos generados antiguos en `obj` y restaurar en un checkout limpio; no convertir el proyecto a SDK como arreglo incidental |
| Fallo de conexión o catálogo incompleto | Revisar servidor, instancia, base, autenticación, conectividad y permisos de lectura de metadatos |

En este equipo existe una copia local de referencias en `packages/Microsoft.NETFramework.ReferenceAssemblies.net452.1.0.3/build/`. Es una dependencia local ignorada por Git, no una garantía de que exista en otro checkout. Para usarla con MSBuild se puede suministrar `TargetFrameworkRootPath` apuntando a `build/`; la carpeta debe contener `.NETFramework/v4.5.2`. Ver [Seguimiento.md](Seguimiento.md) para comandos efectivamente probados y sus resultados.

Comando verificado en este equipo, desde la raíz, con VS 2022 Community y los paquetes ya disponibles:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$referenceRoot = Join-Path (Get-Location) 'packages\Microsoft.NETFramework.ReferenceAssemblies.net452.1.0.3\build\'
$testReferences = 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PublicAssemblies'
& $msbuild .\OpenDBDiff.sln /m /p:Configuration=Debug '/p:Platform=Any CPU' `
    "/p:TargetFrameworkRootPath=$referenceRoot" "/p:ReferencePath=$testReferences" `
    /p:ResolveNuGetPackages=false /verbosity:minimal /nologo
```

`ResolveNuGetPackages=false` evitó procesar assets residuales de `obj` en estos proyectos clásicos; no restaura paquetes ni sustituye `nuget restore`. Usarlo solamente con las dependencias ya restauradas. Este ajuste de invocación no modifica los `.csproj` ni su framework.


## Tablas optimizadas para memoria y probes de integración

Schema separa `Tables` y `Memory-Optimized Tables` con conteos visibles/total. Una dependencia o diferencia no soportada conserva el árbol, muestra el motivo y bloquea la exportación completa. Excluir la tabla afectada y volver a generar permite trabajar con los demás objetos. Una selección vacía conserva la semántica anterior: significa todos los objetos. Detalles: [TablasEnMemoria.md](TablasEnMemoria.md).

Los siguientes probes son programas independientes, no pruebas automáticas de MSTest. Compilar primero la solución Debug con el comando anterior. Se necesitan el compilador .NET Framework y SQL Server local con autenticación integrada. No contienen credenciales.

### Laboratorio desechable

El probe crea dos bases nuevas con prefijo único `OpenDBDiffMemoryTest_<guid>`, agrega sus contenedores de memoria en el directorio predeterminado de SQL Server y las elimina en `finally`. No usa CuriosityPOS ni CuriosityPROD. Requiere permisos para crear/eliminar bases y soporte In-Memory OLTP; el servicio SQL necesita acceso a su directorio de datos. Si aparece `Cleanup required`, eliminar únicamente las bases con los nombres exactos reportados después de revisarlos.

Comprueba carga posterior a `Transaction`, claves HASH y compuestas, UNIQUE/CHECK/FK, creación, ALTER, DEFAULT con filas existentes, preservación de datos, eliminación, segunda comparación sin diferencias, bloqueos y conservación del archivo de salida CLI.

```powershell
New-Item -ItemType Directory -Path .\TestResults -Force | Out-Null
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo `
    '/out:TestResults\MemoryLab.exe' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.SqlServer.Schema.dll' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.Abstractions.Schema.dll' `
    '.\OpenDBDiff.Tests\Integration\MemoryLab.cs'
if ($LASTEXITCODE -ne 0) { throw 'Fallo al compilar MemoryLab' }
& .\TestResults\MemoryLab.exe '.\OpenDBDiff\bin\Debug' '.\TestResults'
if ($LASTEXITCODE -ne 0) { throw 'Fallo en MemoryLab; revisar salida y limpieza' }
```

### Panel Schema con las bases locales

Este probe lee CuriosityPOS y CuriosityPROD, contrasta los conteos con `sys.tables`, instancia el control WinForms, verifica grupos y selección y lo renderiza sin mostrar una ventana. Solo consulta esas bases; no aplica scripts. También informa los bloqueos de generación detectados. La imagen y los ejecutables quedan en `TestResults/`, ignorado por Git.

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo `
    '/out:TestResults\MemoryGuiProbe.exe' '/r:System.Windows.Forms.dll' '/r:System.Drawing.dll' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.SqlServer.Schema.dll' `
    '/r:OpenDBDiff\bin\Debug\OpenDBDiff.Abstractions.Schema.dll' `
    '.\OpenDBDiff.Tests\Integration\MemoryGuiProbe.cs'
if ($LASTEXITCODE -ne 0) { throw 'Fallo al compilar MemoryGuiProbe' }
& .\TestResults\MemoryGuiProbe.exe '.\OpenDBDiff\bin\Debug' '.\TestResults\memory-schema.png'
if ($LASTEXITCODE -ne 0) { throw 'Fallo en MemoryGuiProbe' }
```

La ejecución local quedó probada en SQL Server 17.0.1145.4. Las reglas para 2014 y 2016 se prueban también unitariamente; no sustituyen ejecutar los probes contra esos motores. La compatibilidad nueva de esta entrega cubre HASH e índices no agrupados; otros tipos de índice de memoria se bloquean explícitamente.
