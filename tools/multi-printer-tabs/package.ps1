param([string]$InnoCompiler)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build.ps1"
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
# Deliberately list every release file. Never package the source folder recursively.
$stage = Join-Path $dist ('portable-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force "$stage\plugin" | Out-Null
foreach ($name in @('OrcaSlicerMultiPrinterTabs.exe','OrcaSlicerMultiPrinterTabs.exe.config','README.md','LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $stage
}
Copy-Item -LiteralPath "$PSScriptRoot\plugin\workspace_bridge.py" -Destination "$stage\plugin"
$zip = Join-Path $dist 'OrcaMultiPrinterTabs-0.9.0-win-x64.zip'
Compress-Archive -Path "$stage\*" -DestinationPath $zip -Force
$artifacts = @($zip)
if ($InnoCompiler) {
    & $InnoCompiler "$PSScriptRoot\installer.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed' }
    $artifacts += Join-Path $dist 'OrcaMultiPrinterTabs-0.9.0-Setup.exe'
}
$artifacts | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), [IO.Path]::GetFileName($_)
} | Set-Content -LiteralPath "$dist\SHA256SUMS.txt" -Encoding ASCII
Write-Output $artifacts
