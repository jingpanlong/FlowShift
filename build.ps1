param([string]$Name='FlowShift241',[switch]$Thin)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Split-Path $compiler
$wpf = Join-Path $framework 'WPF'
$out = Join-Path (Split-Path $root) ($Name+'.exe')
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Drawing.dll','System.Windows.Forms.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Web.Extensions.dll','System.Xml.dll','Microsoft.CSharp.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$references += @('PresentationCore.dll','PresentationFramework.dll','WindowsBase.dll','UIAutomationProvider.dll','UIAutomationTypes.dll') | ForEach-Object { '/r:' + (Join-Path $wpf $_) }
$payload = Join-Path $root 'core-payload.zip'
$resources = @("/resource:$(Join-Path $root 'FlowShift.ico'),FlowShift.AppIcon")
if (-not $Thin -and (Test-Path -LiteralPath $payload)) { $resources += "/resource:$payload,FlowShift.CorePayload" }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$out" "/win32manifest:$(Join-Path $root 'app.manifest')" "/win32icon:$(Join-Path $root 'FlowShift.ico')" @references @resources (Join-Path $root 'AssemblyInfo.cs') (Join-Path $root 'FlowShift.cs') (Join-Path $root 'Backend.cs') (Join-Path $root 'FloatingDock.cs') (Join-Path $root 'GestureHook.cs') (Join-Path $root 'EdgeDock.cs') (Join-Path $root 'TriggerSettings.cs') (Join-Path $root 'AboutWindow.cs') (Join-Path $root 'DesktopIntegration.cs') (Join-Path $root 'InstanceInbox.cs') (Join-Path $root 'NativeGesture.cs') (Join-Path $root 'AppVisual.cs') (Join-Path $root 'DesktopTests.cs') (Join-Path $root 'RadialCompass.cs') (Join-Path $root 'WheelTests.cs') (Join-Path $root 'WheelPresets.cs') (Join-Path $root 'WheelRegression.cs') (Join-Path $root 'WheelNavigation.cs') (Join-Path $root 'WheelNavigationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
if ($Thin) { Write-Host "Built $out (requires existing runtime-2.0.0.0 cache)"; return }
$buildDir=Join-Path $root '.build'
New-Item -ItemType Directory -Force $buildDir | Out-Null
$uninstaller=Join-Path $buildDir 'Uninstall.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$uninstaller" "/win32manifest:$(Join-Path $root 'app.manifest')" "/win32icon:$(Join-Path $root 'FlowShift.ico')" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll (Join-Path $root 'AssemblyInfo.cs') (Join-Path $root 'Installer.cs') (Join-Path $root 'DesktopIntegration.cs')
if ($LASTEXITCODE -ne 0) { throw 'Uninstaller compilation failed' }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$(Join-Path (Split-Path $root) ($Name+'-Setup.exe'))" "/win32manifest:$(Join-Path $root 'app.manifest')" "/win32icon:$(Join-Path $root 'FlowShift.ico')" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll "/resource:$out,FlowShift.Payload" "/resource:$uninstaller,FlowShift.Uninstaller" (Join-Path $root 'AssemblyInfo.cs') (Join-Path $root 'Installer.cs') (Join-Path $root 'DesktopIntegration.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
Write-Host "Built $Name.exe and $Name-Setup.exe"
