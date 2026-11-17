# Requires .NET 10 SDK. Pure PE-metadata validation (no game DLL execution).
param(
    [Parameter(Mandatory=$true)][string]$DllPath,
    [Parameter(Mandatory=$true)][ValidateSet(0,5)][int]$ExpectedCount,
    [Parameter(Mandatory=$true)][string]$BuildId
)
$ErrorActionPreference='Stop'
$tool = Join-Path $PSScriptRoot 'DevActionVerifier\DevActionVerifier.csproj'
if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) { throw "Missing DLL: $DllPath" }
& dotnet run --project $tool -c Release -- $DllPath $ExpectedCount '1.1.0' $BuildId
if ($LASTEXITCODE -ne 0) { throw "DebugAction/Version/BUILD_ID metadata verification failed for $DllPath" }
