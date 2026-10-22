param([string]$WorkDirectory)
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$taskRoot=[IO.Path]::GetFullPath($WorkDirectory)
$manifest=Get-Content (Join-Path (Split-Path $PSScriptRoot) 'Vendor\Filee\src\Filee.Engines\Infrastructure\engines.json') -Raw | ConvertFrom-Json
$destination=Join-Path $taskRoot 'bundled-engines'
$cache=Join-Path $taskRoot 'engine-downloads'
New-Item -ItemType Directory -Force $destination,$cache | Out-Null
function Download-Component([string]$id){
 $component=$manifest.components.$id
 $file=Join-Path $cache ([Uri]::UnescapeDataString(([Uri]$component.url).Segments[-1]))
 if(-not (Test-Path $file) -or (Get-FileHash $file).Hash -ne $component.sha256){Invoke-WebRequest $component.url -OutFile $file}
 if((Get-FileHash $file).Hash -ne $component.sha256){throw "SHA-256 mismatch: $id"}
 return $file
}
$rhwp=Download-Component 'rhwp'
$rhwpStage=Join-Path $cache ('rhwp-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive $rhwp $rhwpStage
$rhwpExe=Get-ChildItem $rhwpStage -Recurse -Filter rhwp.exe | Select-Object -First 1
$rhwpOut=Join-Path $destination 'rhwp'
New-Item -ItemType Directory -Force $rhwpOut | Out-Null
Get-ChildItem $rhwpExe.DirectoryName -Force | Copy-Item -Destination $rhwpOut -Recurse -Force
$runtime=Download-Component 'vcruntime'
$runtimeStage=Join-Path $cache ('vc-'+[Guid]::NewGuid().ToString('N'))
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($runtime,$runtimeStage)
$tar=Get-ChildItem $runtimeStage -Filter 'pkg-*.tar.zst' | Select-Object -First 1
& (Join-Path $env:WINDIR 'System32\tar.exe') -xf $tar.FullName -C $runtimeStage
if($LASTEXITCODE -ne 0){throw 'C++ runtime extraction failed'}
$runtimeOut=Join-Path $destination 'vcruntime'
New-Item -ItemType Directory -Force $runtimeOut | Out-Null
Get-ChildItem (Join-Path $runtimeStage 'Library\bin') -Filter '*.dll' | Copy-Item -Destination $runtimeOut -Force
Get-ChildItem $runtimeOut -Filter '*.dll' | Copy-Item -Destination $rhwpOut -Force
$seven=Download-Component '7zip'
$sevenStage=Join-Path $cache ('7z-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $sevenStage | Out-Null
$process=Start-Process msiexec.exe -ArgumentList @('/a',('"'+$seven+'"'),'/qn',('TARGETDIR="'+$sevenStage+'"')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw ('7-Zip administrative extraction failed: '+$process.ExitCode)}
$sevenExe=Get-ChildItem $sevenStage -Recurse -Filter 7z.exe | Select-Object -First 1
$sevenOut=Join-Path $destination '7zip'
New-Item -ItemType Directory -Force $sevenOut | Out-Null
foreach($name in @('7z.exe','7z.dll','License.txt','readme.txt')){Copy-Item (Join-Path $sevenExe.DirectoryName $name) $sevenOut -Force}
Write-Host 'Pinned rhwp, 7-Zip and C++ runtime prepared.'
