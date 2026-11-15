$ErrorActionPreference='Stop'
$destination=Join-Path $PSScriptRoot 'work\webview2'
if((Test-Path "$destination\Microsoft.Web.WebView2.Core.dll") -and (Test-Path "$destination\Microsoft.Web.WebView2.WinForms.dll") -and (Test-Path "$destination\WebView2Loader.dll")){return}
New-Item -ItemType Directory -Force $destination | Out-Null
$archive=Join-Path $destination 'webview2.zip'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4314.35/microsoft.web.webview2.1.0.4314.35.nupkg' -OutFile $archive
Add-Type -AssemblyName System.IO.Compression.FileSystem
$unpack=Join-Path $destination 'package'
if(!(Test-Path $unpack)){[IO.Compression.ZipFile]::ExtractToDirectory($archive,$unpack)}
Copy-Item "$unpack\lib\net462\Microsoft.Web.WebView2.Core.dll" $destination -Force
Copy-Item "$unpack\lib\net462\Microsoft.Web.WebView2.WinForms.dll" $destination -Force
Copy-Item "$unpack\runtimes\win-x64\native\WebView2Loader.dll" $destination -Force
Copy-Item "$unpack\LICENSE.txt" "$destination\WebView2-LICENSE.txt" -Force
