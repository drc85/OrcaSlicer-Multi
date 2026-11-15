param([string]$OutputName='OrcaSlicerMultiPrinterTabs.exe')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
& "$root\restore-webview.ps1"
$deps=Join-Path $root 'work\webview2'
$icon=Join-Path $root '..\..\resources\images\OrcaSlicer.ico'
if(!(Test-Path -LiteralPath $icon)){throw 'Build from tools/multi-printer-tabs in the complete repository checkout.'}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ "/out:$root\$OutputName" "/win32manifest:$root\src\app.manifest" "/win32icon:$icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/reference:$deps\Microsoft.Web.WebView2.Core.dll" "/reference:$deps\Microsoft.Web.WebView2.WinForms.dll" "$root\src\Program.cs"
if($LASTEXITCODE -ne 0){throw 'Build failed'}
foreach($file in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll')){Copy-Item -LiteralPath "$deps\$file" -Destination $root -Force}
Copy-Item -LiteralPath "$root\OrcaMultiPrinterTabs.exe.config" -Destination "$root\$OutputName.config" -Force
