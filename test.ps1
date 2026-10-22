param([string]$TestDirectory = (Join-Path $env:TEMP ('FlowShift-tests-' + [Guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
$app = Join-Path (Split-Path $PSScriptRoot) 'FlowShift2.exe'
if (-not (Test-Path -LiteralPath $app)) { & (Join-Path $PSScriptRoot 'build.ps1') }
if (Test-Path -LiteralPath $TestDirectory) { throw 'Please specify a new empty test directory.' }
$process = Start-Process -FilePath $app -ArgumentList @('--self-test', ('"' + $TestDirectory + '"')) -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath (Join-Path $TestDirectory 'test-results.txt')
if ($process.ExitCode -ne 0) { throw ('Self-test failed. Artifacts: ' + $TestDirectory) }
Write-Host ('Self-test passed. Artifacts: ' + $TestDirectory)
