param([string]$OutputName = 'OrcaSlicerMultiPrinterTabs.exe')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$root\$OutputName" "/win32manifest:$root\src\app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "$root\src\Program.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item -LiteralPath "$root\OrcaMultiPrinterTabs.exe.config" -Destination "$root\$OutputName.config" -Force
Write-Output "Built $OutputName"
