# PORT17-V110: strict Debug/Release builds, metadata validation, 1.7-only install.
# Examples:
#   .\BuildAndInstall_RW17.ps1 -BuildOnly -ReferencesDir 'C:\Path\To\RW17\Assemblies'
#   .\BuildAndInstall_RW17.ps1 -ModRoot 'C:\Path\To\Mods\QoLEasyReading' -ReferencesDir 'C:\Path\To\RW17\Assemblies'
#   .\BuildAndInstall_RW17.ps1 -InstallDebug -ModRoot 'C:\Path\To\Mods\QoLEasyReading' -ReferencesDir 'C:\Path\To\RW17\Assemblies'
# 1.6/Assemblies, common About.xml and workshop ID are never modified.
param(
    [string] $ModRoot,
    [string] $ReferencesDir,
    [string] $HarmonyDllPath,
    [switch] $InstallDebug,
    [switch] $BuildOnly
)
$ErrorActionPreference = 'Stop'
$buildId = 'ER-RW17-S3QC-HF1-20261117-R1'
$modVersion = '1.1.0'
$project = Join-Path $PSScriptRoot 'QoLEasyReading.csproj'
$manifestPath = Join-Path $PSScriptRoot 'PORT17_SOURCE_MANIFEST.json'
$recordPath = Join-Path $PSScriptRoot 'PORT17_LAST_BUILD_INSTALL_LOG.json'
$debugLog = Join-Path $PSScriptRoot 'PORT17_BUILD_Debug.log'
$releaseLog = Join-Path $PSScriptRoot 'PORT17_BUILD_Release.log'
$debugDll = Join-Path $PSScriptRoot 'bin\Debug\QoLEasyReading.dll'
$releaseDll = Join-Path $PSScriptRoot 'bin\Release\QoLEasyReading.dll'
$verifier = Join-Path $PSScriptRoot 'Port17_Reports\PORT17_V110_RELDIAG1\VerifyDeveloperActions_RW17.ps1'
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Missing 1.7 project: $project" }
if (-not (Test-Path -LiteralPath $verifier -PathType Leaf)) { throw "Missing metadata verifier: $verifier" }
if ($BuildOnly -and $InstallDebug) { throw 'Do not combine -BuildOnly and -InstallDebug.' }
if (-not $BuildOnly -and [string]::IsNullOrWhiteSpace($ModRoot)) {
    throw 'Provide -ModRoot or choose -BuildOnly; no files installed.'
}

# Validate authoritative project contents, not a source glob.
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$frameworks = @($projectXml.Project.PropertyGroup | ForEach-Object { $_.TargetFramework })
if ($frameworks -notcontains 'net10.0') { throw 'Expected net10.0 target framework.' }
$automatic = @($projectXml.Project.PropertyGroup | ForEach-Object { $_.EnableDefaultCompileItems })
if ($automatic -notcontains 'false') { throw 'Expected EnableDefaultCompileItems=false.' }
$groups = @($projectXml.Project.ItemGroup)
$debugOnly = @('Diagnostics\SchematicResearchAudit.cs',
    'Diagnostics\SchematicLiveResearchProbe.cs',
    'Diagnostics\SchematicIconAssetAudit.cs',
    'Diagnostics\SchematicOwnershipAudit.cs',
    'Diagnostics\SchematicOwnershipTraceLogger.cs',
    'Diagnostics\SchematicTradeGroupDiagnostics.cs',
    'Diagnostics\SchematicTradeGroupRegressionAudit.cs',
    'Diagnostics\SchematicTradeGroupTestFixtures.cs')
$common = @()
$conditional = @()
foreach ($group in $groups) {
    $items = @($group.Compile | Where-Object { $_ -ne $null } | ForEach-Object { [string]$_.Include })
    if ($items.Count -eq 0) { continue }
    if ([string]::IsNullOrWhiteSpace([string]$group.Condition)) {
        $common += $items
    } elseif ([string]$group.Condition -eq "'`$(Configuration)' == 'Debug'") {
        $conditional += $items
    } else {
        throw "Unexpected conditional Compile group: $($group.Condition)"
    }
}
$common = @($common); $conditional = @($conditional)
$all = @($common + $conditional)
if ($common.Count -ne 55 -or $conditional.Count -ne 8 -or
    $all.Count -ne 63 -or @($all | Sort-Object -Unique).Count -ne 63) {
    throw "Unexpected source contract: common=$($common.Count), debugOnly=$($conditional.Count), total=$($all.Count)."
}
foreach ($source in $all) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $source) -PathType Leaf)) {
        throw "Missing registered source: $source"
    }
}
foreach ($source in $debugOnly) {
    if ($conditional -notcontains $source -or $common -contains $source) {
        throw "Debug-only CS incorrectly registered: $source"
    }
}
foreach ($legacy in @('Diagnostics\ReadingJobAudit.cs','Diagnostics\ManualReadingScheduleTrace.cs')) {
    if ($all -contains $legacy) { throw "Legacy CS unexpectedly compiled: $legacy" }
}
foreach ($production in @('Core\EasyReadingRuntimeHealth.cs',
    'Books\BookTradeSkillIcons.Row.cs','Books\BookTradeSkillIcons.Paint.cs',
    'Schematic\SchematicOwnershipTrace.cs','Schematic\SchematicTradeOwnershipSession.cs',
    'Settings\EasyReadingSettingsWindow.cs','Schematic\SchematicTradeGroupDecisionPolicy.cs')) {
    if ($common -notcontains $production) { throw "Production CS is not shared: $production" }
}
$sourceIdentity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Core\EasyReadingBuildIdentity.cs') -Raw
if (-not $sourceIdentity.Contains($buildId) -or -not $sourceIdentity.Contains('ModVersion = "1.1.0"')) {
    throw '1.7 BUILD_ID / ModVersion mismatch.'
}

# Reference About.xml records public metadata but does not rewrite shared game About metadata.
function Assert-About([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing About.xml: $Path" }
    [xml]$about = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if ([string]$about.ModMetaData.packageId -ne 'zcn.qol.easyreading') { throw "Wrong packageId in $Path" }
    if ([string]$about.ModMetaData.name -ne 'QoL: Easy Reading') { throw "Unexpected title in $Path (never append [v1.1.0])" }
    if (-not ([string]$about.ModMetaData.description).Contains('Version: v1.1.0')) {
        throw "Missing public version in $Path"
    }
    $versions = @($about.ModMetaData.supportedVersions.li | ForEach-Object { [string]$_ })
    if ($versions -notcontains '1.7' -or $versions -notcontains '1.6') {
        throw "Expected existing 1.6+1.7 supportedVersions in $Path"
    }
}
Assert-About (Join-Path $PSScriptRoot 'About\About.xml')

$assets = @()
$assets += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Textures') -Filter '*.png' -File -Recurse)
$assets += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Languages') -Filter '*.xml' -File -Recurse)
if (@($assets | Where-Object { $_.Extension -eq '.png' }).Count -ne 17) { throw 'Expected exactly 17 existing RW17 PNGs.' }
if (@($assets | Where-Object { $_.Extension -eq '.xml' }).Count -ne 2) { throw 'Expected exactly 2 EN/KO XML files.' }
foreach ($name in @('Schematic_Review.png','Schematic_Unknown.png',
    'Schematic_Complete.png','Schematic_DuplicateWarning.png')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot "Textures\QoLEasyReading\Schematic\$name") -PathType Leaf)) {
        throw "Missing Schematic asset: $name"
    }
}

$manifestFiles = @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Recurse | Where-Object {
    $_.Extension -in @('.cs','.csproj','.ps1','.png','.xml') -and
    $_.FullName -notmatch '[\\/](bin|obj|Port17_InstallBackups)[\\/]'
} | Sort-Object FullName)
$sourceHashes = @($manifestFiles | ForEach-Object {
    [ordered]@{ relativePath=$_.FullName.Substring($PSScriptRoot.Length+1).Replace('\','/');
        sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
})
[ordered]@{ buildId=$buildId;modVersion=$modVersion;target='RW1.7 / net10.0 / Harmony 3';
    productionCompileCs=55;debugOnlyCompileCs=8;debugCompileCs=63;releaseCompileCs=55;
    expectedDebugActions=5;expectedReleaseActions=0;sharedAbout='PRESERVED';
    assetPolicy='17 existing RW17 PNG + 2 language XML';sourceFiles=$sourceHashes
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
Write-Host "[PORT17-V110][SOURCE_AUDIT] PASS; common=55; debugOnly=8; Debug=63; Release=55; assets=17PNG/2XML; sourceManifestSha256=$manifestHash"

$argProps = @()
if (-not [string]::IsNullOrWhiteSpace($ReferencesDir)) {
    $refs = [IO.Path]::GetFullPath($ReferencesDir)
    foreach ($dll in @('Assembly-CSharp.dll','UnityStubs.dll','Waylith.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $refs $dll) -PathType Leaf)) {
            throw "Missing required 1.7 reference $dll in $refs"
        }
    }
    $argProps += "-p:RimWorld17ManagedDir=$refs"
    if ([string]::IsNullOrWhiteSpace($HarmonyDllPath) -and
        (Test-Path -LiteralPath (Join-Path $refs '0Harmony.dll') -PathType Leaf)) {
        $HarmonyDllPath = Join-Path $refs '0Harmony.dll'
    }
}
if (-not [string]::IsNullOrWhiteSpace($HarmonyDllPath)) {
    $harmonyPath = [IO.Path]::GetFullPath($HarmonyDllPath)
    if (-not (Test-Path -LiteralPath $harmonyPath -PathType Leaf)) { throw "Missing 1.7 Harmony 3 DLL: $harmonyPath" }
    $argProps += "-p:Harmony17DllPath=$harmonyPath"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK dotnet not found.' }
$sdk = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0 -or -not $sdk.StartsWith('10.')) { throw "Expected .NET 10 SDK, found $sdk" }

function Invoke-CheckedBuild([string]$configuration,[string]$logPath) {
    Write-Host "[PORT17-V110][BUILD] configuration=$configuration; buildId=$buildId"
    $buildArgs = @('build',$project,'-c',$configuration,'-t:Rebuild','--nologo',
       '--verbosity','minimal','-fl',"-flp:logfile=$logPath;verbosity=normal") + $argProps
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Rebuild failed: configuration=$configuration; log=$logPath; no install" }
}
Invoke-CheckedBuild 'Debug' $debugLog
Invoke-CheckedBuild 'Release' $releaseLog
# Non-executing PE metadata check catches leaked developer actions and stale Release DLL.
& $verifier -DllPath $debugDll -ExpectedCount 5 -BuildId $buildId
if ($LASTEXITCODE -ne 0) { throw 'Debug DLL metadata gate failed.' }
& $verifier -DllPath $releaseDll -ExpectedCount 0 -BuildId $buildId
if ($LASTEXITCODE -ne 0) { throw 'Release DLL metadata gate failed.' }
$debugHash = (Get-FileHash -LiteralPath $debugDll -Algorithm SHA256).Hash
$releaseHash = (Get-FileHash -LiteralPath $releaseDll -Algorithm SHA256).Hash
if ($debugHash -eq $releaseHash) { throw 'Debug and Release DLL hashes identical; investigate build configuration.' }
$selectedConfiguration = if ($InstallDebug) { 'Debug' } else { 'Release' }
$record = [ordered]@{
    modVersion=$modVersion;buildId=$buildId;target='RW1.7';timestamp=(Get-Date -Format 'o')
    configuration=$selectedConfiguration;sdkVersion=$sdk;debugCompileCs=63;releaseCompileCs=55;
    expectedDebugActions=5;expectedReleaseActions=0;sourceManifestSha256=$manifestHash
    debugSha256=$debugHash;releaseSha256=$releaseHash;
    debugBuildLog=$debugLog;releaseBuildLog=$releaseLog;
    status='BUILT_METADATA_VERIFIED_NOT_INSTALLED';installed=$false;rollback='NOT_REQUIRED';
    files=@();backupDir=$null
}
if ($BuildOnly) {
    $record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding UTF8
    Write-Host "[PORT17-V110][BUILD] PASS; metadata=Debug5/Release0; BuildOnly; sha256(Release)=$releaseHash"
    return
}

# Shared metadata must already agree; the RW17 installer does not modify common About.xml.
$modRootFull = [IO.Path]::GetFullPath($ModRoot)
Assert-About (Join-Path $modRootFull 'About\About.xml')
$selectedDll = if ($InstallDebug) { $debugDll } else { $releaseDll }
$targets = @([pscustomobject]@{name='RW17_DLL';src=$selectedDll;dst=(Join-Path $modRootFull '1.7\Assemblies\QoLEasyReading.dll')})
foreach ($asset in $assets) {
    $rel = $asset.FullName.Substring($PSScriptRoot.Length + 1)
    $targets += [pscustomobject]@{name=$rel;src=$asset.FullName;dst=(Join-Path $modRootFull $rel)}
}
$olderDll = Join-Path $modRootFull '1.6\Assemblies\QoLEasyReading.dll'
$olderBefore = if (Test-Path -LiteralPath $olderDll -PathType Leaf) {
    (Get-FileHash -LiteralPath $olderDll -Algorithm SHA256).Hash
} else { $null }
$backupDir = Join-Path $PSScriptRoot ('Port17_InstallBackups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$token = [guid]::NewGuid().ToString('N')
$work = @();$commit=$false
try {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    for ($i=0; $i -lt $targets.Count; $i++) {
        $e=$targets[$i];$expected=(Get-FileHash -LiteralPath $e.src -Algorithm SHA256).Hash
        New-Item -ItemType Directory -Path (Split-Path -Parent $e.dst) -Force | Out-Null
        $existed=Test-Path -LiteralPath $e.dst -PathType Leaf
        $oldHash=if ($existed) {(Get-FileHash -LiteralPath $e.dst -Algorithm SHA256).Hash} else {$null}
        $backup=if ($existed) {Join-Path $backupDir ("$i-"+[IO.Path]::GetFileName($e.dst))} else {$null}
        if ($existed) {
            Copy-Item -LiteralPath $e.dst -Destination $backup -ErrorAction Stop
            if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $oldHash) {throw "Backup hash mismatch: $($e.dst)"}
        }
        $stage=$e.dst+'.rw17stage-'+$token
        $work += [pscustomobject]@{name=$e.name;dst=$e.dst;expected=$expected;existed=$existed;oldHash=$oldHash;backup=$backup;stage=$stage}
        Copy-Item -LiteralPath $e.src -Destination $stage -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $stage -Algorithm SHA256).Hash -ne $expected) {throw "Staged hash mismatch: $stage"}
    }
    $commit=$true
    foreach ($e in $work) {
        Copy-Item -LiteralPath $e.stage -Destination $e.dst -Force -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $e.dst -Algorithm SHA256).Hash -ne $e.expected) {throw "Install hash mismatch: $($e.dst)"}
    }
    $olderAfter=if (Test-Path -LiteralPath $olderDll -PathType Leaf) {
        (Get-FileHash -LiteralPath $olderDll -Algorithm SHA256).Hash
    } else {$null}
    if ($olderBefore -ne $olderAfter) {throw '1.6 DLL changed during RW17 installation.'}
    $record.status='INSTALLED_SHA_VERIFIED';$record.installed=$true
    Write-Host "[PORT17-V110][INSTALL] PASS; configuration=$selectedConfiguration; 1.7 DLL and 17 PNG/2 XML verified; 1.6 DLL unchanged."
} catch {
    $failure=$_.Exception.Message;$rollbackErrors=@()
    if ($commit) {
        foreach ($e in $work) {
            try {
                if ($e.existed) {
                    Copy-Item -LiteralPath $e.backup -Destination $e.dst -Force -ErrorAction Stop
                    if ((Get-FileHash -LiteralPath $e.dst -Algorithm SHA256).Hash -ne $e.oldHash) {throw 'Rollback hash mismatch'}
                } elseif (Test-Path -LiteralPath $e.dst -PathType Leaf) {
                    Remove-Item -LiteralPath $e.dst -Force -ErrorAction Stop
                }
            } catch {$rollbackErrors += "$($e.dst): $($_.Exception.Message)"}
        }
    }
    $record.rollback=if ($commit) {if ($rollbackErrors.Count -eq 0) {'RESTORED_ALL'} else {'FAILED_OR_UNCERTAIN'}} else {'NOT_NEEDED_PRECOMMIT'}
    $record.status='INSTALL_FAILED: '+$failure;$record.rollbackErrors=$rollbackErrors
    throw "RW17 install failed; rollback=$($record.rollback); $failure"
} finally {
    foreach ($e in $work) {
        if (Test-Path -LiteralPath $e.stage -PathType Leaf) {
            Remove-Item -LiteralPath $e.stage -Force -ErrorAction SilentlyContinue
        }
    }
    $record.backupDir=$backupDir
    $record.files=@($work | ForEach-Object { [ordered]@{name=$_.name;target=$_.dst;sha256=$_.expected;backup=$_.backup;oldSha256=$_.oldHash} })
    $record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding UTF8
}
Write-Host "[PORT17-V110][INSTALL] log=$recordPath; backups=$backupDir"
