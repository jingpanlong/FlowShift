param([string]$Dotnet='dotnet', [switch]$SkipEngineDownloads)
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$root=$PSScriptRoot
$build=Join-Path $root '.build'
New-Item -ItemType Directory -Force $build | Out-Null
$env:DOTNET_CLI_HOME=Join-Path $build 'dotnet-home'
$env:NUGET_PACKAGES=Join-Path $build 'nuget'
$publish=Join-Path $build ('runtime-'+[Guid]::NewGuid().ToString('N'))
& $Dotnet publish (Join-Path $root 'Bridge\FlowShift.CoreHost.csproj') -c Release -r win-x64 --self-contained true -o $publish -p:DebugType=None -p:GenerateDocumentationFile=false -p:NuGetAudit=false --nologo
if($LASTEXITCODE -ne 0){throw 'Core build failed'}
& (Join-Path $root 'Build\fetch-bundled.ps1') -WorkDirectory $build
New-Item -ItemType Directory -Force (Join-Path $publish 'engines') | Out-Null
Copy-Item (Join-Path $build 'bundled-engines\*') (Join-Path $publish 'engines') -Recurse -Force
Copy-Item (Join-Path $build 'bundled-engines\vcruntime\*.dll') $publish -Force
if(-not $SkipEngineDownloads){
 $env:FLOWSHIFT_DATA=Join-Path $build 'engine-data'
 foreach($package in @('ffmpeg','pandoc','ghostscript','calibre','libreoffice')){
  & (Join-Path $publish 'FlowShift.CoreHost.exe') install $package
  if($LASTEXITCODE -ne 0){throw ('Cannot install engine: '+$package)}
 }
 Get-ChildItem (Join-Path $env:FLOWSHIFT_DATA 'engines') -Directory | Where-Object { -not $_.Name.StartsWith('.') } | Copy-Item -Destination (Join-Path $publish 'engines') -Recurse -Force
}
$licenses=Join-Path $publish 'licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
Copy-Item (Join-Path $root 'Vendor\Filee\LICENSE') (Join-Path $licenses 'Filee-LICENSE.txt') -Force
Copy-Item (Join-Path $root 'Vendor\Filee\THIRD-PARTY-NOTICES.md') $licenses -Force
Copy-Item (Join-Path $root 'LICENSE.txt'),(Join-Path $root 'THIRD-PARTY-NOTICES.txt') $licenses -Force
$payload=Join-Path $root 'core-payload.zip'
if(Test-Path $payload){Remove-Item -LiteralPath $payload -Force}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($publish,$payload,[IO.Compression.CompressionLevel]::Optimal,$false)
& (Join-Path $root 'build.ps1')
Write-Host 'Full single-file application and installer built.'
