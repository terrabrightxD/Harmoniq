param([string]$Executable = "$PSScriptRoot/Windows/CrateScope.exe")
$ErrorActionPreference = 'Stop'
if (-not (Test-Path "$PSScriptRoot/fixtures/triad_0_major.wav")) { & "$PSScriptRoot/GenerateFixtures.ps1" }
$report = Join-Path $PSScriptRoot ('verification-' + [DateTime]::Now.Ticks + '.json')
$files = Get-ChildItem "$PSScriptRoot/fixtures/*.wav" | Select-Object -ExpandProperty FullName
$arguments = @('--analyze', $report) + $files
$quoted = $arguments | ForEach-Object { '"' + $_ + '"' }
$process = Start-Process -FilePath $Executable -ArgumentList $quoted -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Analyseprozess: $($process.ExitCode)" }
$actual = Get-Content $report -Raw | ConvertFrom-Json
$expected = Import-Csv "$PSScriptRoot/expected.csv"
$rows = foreach ($e in $expected) {
 $r = $actual.results | Where-Object Name -eq $e.name
 [PSCustomObject]@{Name=$e.name; ExpectedKey=$e.expected_key; Key=$r.Key; ExpectedBpm=$e.expected_bpm; Bpm=$r.Bpm; KeyOK=($r.Key -eq $e.expected_key); BpmOK=([Math]::Abs($r.Bpm-[double]$e.expected_bpm) -le 1)}
}
$rows | Export-Csv "$PSScriptRoot/comparison.csv" -NoTypeInformation
if (($rows | Where-Object { -not $_.KeyOK -or -not $_.BpmOK }).Count -gt 0) { throw 'Abweichungen im synthetischen Testbestand; siehe comparison.csv' }
Write-Output "PASS: $($rows.Count) bekannte Tonarten und Tempi; $($actual.results.Count) Dateien analysiert. Bericht: $report"
