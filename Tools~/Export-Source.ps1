param([string]$OutputDirectory, [switch]$NoZip)
$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$metadata = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $packageRoot 'Artifacts~' }
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$name = 'RippleGameBase-' + $metadata.version + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$destination = Join-Path $outputRoot $name
[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$allowedRoots = @('Runtime', 'Editor', 'Tests', 'Documentation~', 'Samples~', 'Tools~', 'Verification~')
$files = @(Get-ChildItem -LiteralPath $packageRoot -File | Where-Object { $_.Name -in @('package.json','README.md','CHANGELOG.md','.gitignore','.gitattributes') -or $_.Extension -eq '.meta' })
foreach ($folder in $allowedRoots) {
    $path = Join-Path $packageRoot $folder
    if (Test-Path -LiteralPath $path) { $files += Get-ChildItem -LiteralPath $path -File -Recurse }
}
foreach ($file in $files) {
    if ($file.Extension -notin '.cs','.json','.asmdef','.meta','.uss','.md','.ps1' -and $file.Name -notin '.gitignore','.gitattributes') { throw ('Unexpected release file: ' + $file.FullName) }
    $target = Join-Path $destination $file.FullName.Substring($packageRoot.Length + 1)
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target)) | Out-Null
    [System.IO.File]::Copy($file.FullName, $target, $false)
    [System.IO.File]::SetAttributes($target, ([System.IO.File]::GetAttributes($target) -band (-bnot [System.IO.FileAttributes]::ReadOnly)))
}
$archive = $null
if (-not $NoZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = $destination + '.zip'
    [System.IO.Compression.ZipFile]::CreateFromDirectory($destination, $archive)
}
[pscustomobject]@{ Folder = $destination; Zip = $archive; Files = $files.Count; Version = $metadata.version }
