$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force "$PSScriptRoot\work" | Out-Null
& "$PSScriptRoot\restore-webview.ps1"
& $compiler /nologo /target:exe /platform:x64 /main:CloneRegressionTests "/out:$PSScriptRoot\work\CloneRegressionTests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/reference:$PSScriptRoot\work\webview2\Microsoft.Web.WebView2.Core.dll" "/reference:$PSScriptRoot\work\webview2\Microsoft.Web.WebView2.WinForms.dll" "$PSScriptRoot\src\Program.cs" "$PSScriptRoot\tests\CloneRegressionTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Clone test compilation failed' }
foreach($file in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll')){Copy-Item -LiteralPath "$PSScriptRoot\work\webview2\$file" -Destination "$PSScriptRoot\work" -Force}
Push-Location $PSScriptRoot
try {
    & "$PSScriptRoot\work\CloneRegressionTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Clone regression test failed' }
} finally { Pop-Location }
