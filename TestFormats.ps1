param([string]$Executable="$PSScriptRoot/Windows/CrateScope.exe")
$ErrorActionPreference='Stop'
if(-not(Test-Path "$PSScriptRoot/fixtures/triad_0_major.wav")){& "$PSScriptRoot/GenerateFixtures.ps1"}
$decoder=Join-Path (Split-Path $Executable) 'ffmpeg.exe'
$temp=Join-Path $PSScriptRoot ('.decoder-cache/formats-'+[DateTime]::Now.Ticks)
New-Item -ItemType Directory -Force $temp|Out-Null
$cases=@(@('pcm16.wav','pcm_s16le'),@('pcm24.wav','pcm_s24le'),@('float.wav','pcm_f32le'),@('lossless.flac','flac'),@('audio.aiff','pcm_s16be'),@('audio.mp3','libmp3lame'),@('aac.m4a','aac'),@('alac.m4a','alac'),@('audio.ogg','libvorbis'),@('audio.opus','libopus'),@('audio.wma','wmav2'),@('audio.aac','aac'))
$files=@()
foreach($c in $cases){$target=Join-Path $temp $c[0]; & $decoder -nostdin -v error -i "$PSScriptRoot/fixtures/triad_0_major.wav" -c:a $c[1] -n $target; if($LASTEXITCODE -ne 0){throw "Encoder failed: $($c[0])"};$files+=$target}
$report=Join-Path $temp 'results.json'
$args=@('--analyze',$report)+$files
$process=Start-Process -FilePath $Executable -ArgumentList ($args|ForEach-Object{'"'+$_+'"'}) -Wait -PassThru -WindowStyle Hidden
$result=Get-Content $report -Raw|ConvertFrom-Json
if($process.ExitCode -ne 0 -or $result.results.Count -ne $cases.Count){throw ($result.errors -join '; ')}
$result.results|Select-Object Name,Key,Bpm,Seconds|Format-Table
if(($result.results|Where-Object{ $_.Key -ne 'C major' -or [Math]::Abs($_.Bpm-120) -gt 1}).Count -gt 0){throw 'Format changed expected key/tempo'}
Write-Output "PASS: $($cases.Count) formats/codecs. Report: $report"
