$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $packageRoot 'VerificationOutput~/Wallet'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$sdk = Get-ChildItem -LiteralPath (Join-Path $env:ProgramFiles 'dotnet/sdk') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$pack = Get-ChildItem -LiteralPath (Join-Path $env:ProgramFiles 'dotnet/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$framework = Get-ChildItem -LiteralPath (Join-Path $pack.FullName 'ref') -Directory | Select-Object -First 1
$assemblyPath = Join-Path $outputDirectory 'WalletChecks.dll'
$arguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', "/out:$assemblyPath")
$arguments += Get-ChildItem -LiteralPath $framework.FullName -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$arguments += Join-Path $packageRoot 'Verification~/WalletChecks.cs'
$arguments += Join-Path $packageRoot 'Runtime/Economy/CurrencyWallet.cs'
$arguments += Join-Path $packageRoot 'Runtime/Economy/ICurrencyBalanceStore.cs'
& dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Wallet verification compilation failed.' }
$runtime = @{ runtimeOptions = @{ tfm = $framework.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $pack.Name } } }
[System.IO.File]::WriteAllText((Join-Path $outputDirectory 'WalletChecks.runtimeconfig.json'), ($runtime | ConvertTo-Json -Depth 5))
& dotnet $assemblyPath
if ($LASTEXITCODE -ne 0) { throw 'Wallet verification failed.' }
