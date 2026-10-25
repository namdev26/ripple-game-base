$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$metadata = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
if ($metadata.name -ne 'com.ripplegame.base') { throw 'Unexpected package identity.' }
if ($metadata.author.name -ne 'RIPPLE Game by Namdev26') { throw 'Author credit missing.' }
if ($metadata.version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a release package version.' }
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot 'Runtime'),(Join-Path $packageRoot 'Editor') -Recurse -File -Filter '*.cs')
$forbidden = 'SnakeGameplayController|SnakeGameSession|foodGoal|game\.progression\.coin_balance|Resources/UI/Popups|Assets/Scrips|"GameplayScene"|"HomeScene"'
foreach ($file in $sourceFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text -match $forbidden) { throw ('Game dependency in ' + $file.FullName) }
    if ($text -match 'ca-app-pub-(?!3940256099942544)[0-9-]+') { throw ('Non-demo ad identifier in ' + $file.FullName) }
    if (-not (Test-Path -LiteralPath ($file.FullName + '.meta'))) { throw ('Missing native Unity metadata: ' + $file.FullName) }
}
$guids = @{}
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter '*.meta' | Where-Object { $_.FullName -notmatch '[\\/](\.git|Artifacts~|VerificationOutput~)[\\/]' }) {
    $match = [regex]::Match([System.IO.File]::ReadAllText($file.FullName), '(?m)^guid:\s*([a-f0-9]{32})')
    if (-not $match.Success) { throw ('Invalid metadata: ' + $file.FullName) }
    $guid = $match.Groups[1].Value
    if ($guids.ContainsKey($guid)) { throw ('Duplicate source GUID: ' + $file.FullName + ' / ' + $guids[$guid]) }
    $guids[$guid] = $file.FullName
}
$snapshot = & (Join-Path $PSScriptRoot 'Export-Source.ps1') -NoZip
$forbiddenFiles = Get-ChildItem -LiteralPath $snapshot.Folder -Recurse -File | Where-Object { $_.Extension -in '.asset','.prefab','.unity','.dll','.exe','.keystore','.jks','.pem' -or $_.Name -in 'google-services.json','GoogleService-Info.plist' }
if (@($forbiddenFiles).Count -gt 0) { throw 'Project-owned or binary content entered the export.' }
Write-Output ('RIPPLE_PACKAGE_BOUNDARY_PASS code_files=' + $sourceFiles.Count + ' unique_guids=' + $guids.Count + ' export_files=' + $snapshot.Files)
