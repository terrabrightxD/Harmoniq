param([string]$Destination="$PSScriptRoot/Windows")
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot '.decoder-cache'
New-Item -ItemType Directory -Force $cache,$Destination | Out-Null
$url='https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
Invoke-WebRequest $url -OutFile "$cache/ffmpeg.zip"
Invoke-WebRequest "$url.sha256" -OutFile "$cache/ffmpeg.sha256"
$expected=((Get-Content "$cache/ffmpeg.sha256" -Raw).Trim() -split '\s+')[0]
if((Get-FileHash "$cache/ffmpeg.zip" -Algorithm SHA256).Hash -ne $expected){throw 'FFmpeg checksum mismatch'}
Expand-Archive "$cache/ffmpeg.zip" "$cache/unpacked" -Force
$binary=Get-ChildItem "$cache/unpacked" -Filter ffmpeg.exe -Recurse | Select-Object -First 1
Copy-Item $binary.FullName "$Destination/ffmpeg.exe"
$root=$binary.Directory.Parent.FullName
New-Item -ItemType Directory -Force "$Destination/ffmpeg-notices"|Out-Null
Copy-Item "$root/LICENSE","$root/README.txt" "$Destination/ffmpeg-notices"
Set-Content "$Destination/ffmpeg-notices/BUILD-SOURCE.txt" "Binary distributor and corresponding FFmpeg source/build information: https://www.gyan.dev/ffmpeg/builds/ . Archive SHA256: $expected . FFmpeg runs as a separate GPLv3 executable."
& "$Destination/ffmpeg.exe" -version | Set-Content "$Destination/ffmpeg-notices/version.txt"
