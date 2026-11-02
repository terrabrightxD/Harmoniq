$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot/tools/FixtureGenerator.cs"
$folder = Join-Path $PSScriptRoot 'fixtures'
New-Item -ItemType Directory -Force $folder | Out-Null
for ($minor=0; $minor -lt 2; $minor++) {
 for ($root=0; $root -lt 12; $root++) {
  $mode=if($minor -eq 1){'minor'}else{'major'}
  [FixtureGenerator]::Write("$folder/triad_${root}_${mode}.wav",$root,($minor -eq 1),120,24,0.7,0,$false)
 }
}
foreach($bpm in @(60,75,90,100,128,140,160,180)){ [FixtureGenerator]::Write("$folder/tempo_$bpm.wav",0,$false,$bpm,24,0.7,0,$false) }
[FixtureGenerator]::Write("$folder/gain_low.wav",0,$false,120,24,0.07,0,$false)
[FixtureGenerator]::Write("$folder/silence_3.wav",0,$false,120,24,0.7,3,$false)
[FixtureGenerator]::Write("$folder/length_48.wav",0,$false,120,48,0.7,0,$false)
[FixtureGenerator]::Write("$folder/pitch_2.wav",2,$false,120,24,0.7,0,$false)
[FixtureGenerator]::Write("$folder/arrangement_C.wav",0,$false,120,48,0.7,0,$true)
[FixtureGenerator]::Write("$folder/arrangement_Am.wav",9,$true,120,48,0.7,0,$true)
