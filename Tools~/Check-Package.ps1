$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$metadata = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
if ($metadata.name -ne 'com.ripplegame.base') { throw 'Unexpected package identity.' }
if ($metadata.author.name -ne 'RIPPLE Game by Namdev26') { throw 'Author credit missing.' }
if ($metadata.version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a release package version.' }
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot 'Runtime'),(Join-Path $packageRoot 'Editor') -Recurse -File -Filter '*.cs')
$forbidden = 'SnakeGameplayController|SnakeGameSession|foodGoal|game\.progression\.coin_balance|Resources/UI/Popups|Assets/Scrips|"GameplayScene"|"HomeScene"'
# SDK key shapes are matched only inside "..." literals of Runtime/Editor code (Tests/ is not scanned).
# MAX SDK key: 86 characters of [A-Za-z0-9_-]. AppsFlyer dev key: a literal of exactly 22 alphanumerics.
$stringLiteral = [regex]'"(?:[^"\\]|\\.)*"'
$maxKeyShape = [regex]'(?<![A-Za-z0-9_-])[A-Za-z0-9_-]{86}(?![A-Za-z0-9_-])'
$appsFlyerKeyShape = [regex]'^"[A-Za-z0-9]{22}"$'
foreach ($file in $sourceFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text -match $forbidden) { throw ('Game dependency in ' + $file.FullName) }
    if ($text -match 'ca-app-pub-(?!3940256099942544)[0-9-]+') { throw ('Non-demo ad identifier in ' + $file.FullName) }
    if (-not (Test-Path -LiteralPath ($file.FullName + '.meta'))) { throw ('Missing native Unity metadata: ' + $file.FullName) }
    foreach ($line in ($text -split "`r?`n")) {
        foreach ($literal in $stringLiteral.Matches($line)) {
            if ($maxKeyShape.IsMatch($literal.Value) -or $appsFlyerKeyShape.IsMatch($literal.Value)) { throw ('Possible SDK key in ' + $file.FullName) }
        }
    }
}
# Every asmdef versionDefines entry for a catalog package must be exactly [<catalog version>].
$catalog = Get-Content -LiteralPath (Join-Path $packageRoot 'Editor/Sdk/SdkCatalog.json') -Raw | ConvertFrom-Json
$catalogVersions = @{}
foreach ($entry in $catalog.entries) { $catalogVersions[$entry.packageName] = $entry.version }
$asmdefCount = 0
foreach ($asmdef in Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter '*.asmdef' | Where-Object { $_.FullName.Substring($packageRoot.Length) -notmatch '[\\/](\.git|\.claude|\.superpowers|Artifacts~|VerificationOutput~)[\\/]' }) {
    $asmdefCount++
    $definition = Get-Content -LiteralPath $asmdef.FullName -Raw | ConvertFrom-Json
    foreach ($versionDefine in @($definition.versionDefines)) {
        if ($null -eq $versionDefine -or -not $catalogVersions.ContainsKey($versionDefine.name)) { continue }
        if ($versionDefine.expression -ne ('[' + $catalogVersions[$versionDefine.name] + ']')) { throw ('versionDefines do not match SdkCatalog: ' + $asmdef.FullName) }
    }
}
if ($asmdefCount -eq 0) { throw 'No asmdef files were checked against SdkCatalog.' }
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
