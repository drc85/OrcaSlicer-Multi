$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force "$PSScriptRoot\work" | Out-Null
& $compiler /nologo /target:exe /platform:x64 /main:CloneRegressionTests "/out:$PSScriptRoot\work\CloneRegressionTests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\src\Program.cs" "$PSScriptRoot\tests\CloneRegressionTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Clone test compilation failed' }
Push-Location $PSScriptRoot
try {
    & "$PSScriptRoot\work\CloneRegressionTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Clone regression test failed' }
} finally { Pop-Location }
