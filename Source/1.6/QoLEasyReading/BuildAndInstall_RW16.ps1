# S3-REL-DIAG1: strict Debug-only diagnostics; Release-only install with rollback.
# Example (RimWorld closed):
# powershell -ExecutionPolicy Bypass -File .\BuildAndInstall_RW16.ps1 -BuildOnly
# powershell -ExecutionPolicy Bypass -File .\BuildAndInstall_RW16.ps1 -ModRoot "C:\...\RimWorld\Mods\QoLEasyReading"
param(
    [string] $ModRoot,
    [switch] $BuildOnly,
    [string] $RimWorldManagedDir,
    [string] $HarmonyDllPath
)

$ErrorActionPreference = 'Stop'
$buildId = 'ER-RW16-S3QC-HF1-20261117-R1'
$modVersion = '1.1.0'
$expectedAssemblyVersion = '1.1.0.0'
$sourceAboutPath = Join-Path $PSScriptRoot 'About\About.xml'
$project = Join-Path $PSScriptRoot 'QoLEasyReading.csproj'
$debugDll = Join-Path $PSScriptRoot 'bin\Debug\QoLEasyReading.dll'
$releaseDll = Join-Path $PSScriptRoot 'bin\Release\QoLEasyReading.dll'
$manifestPath = Join-Path $PSScriptRoot 'S3QC_HF1_SOURCE_MANIFEST.json'
$recordPath = Join-Path $PSScriptRoot 'S3QC_HF1_LAST_BUILD_INSTALL_LOG.json'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "S3-REL-DIAG1 project missing: $project" }
# Fail closed when the About metadata and requested public mod version are inconsistent.
if (-not (Test-Path -LiteralPath $sourceAboutPath -PathType Leaf)) {
    throw "v1.1.0 About.xml missing: $sourceAboutPath; no installation"
}
[xml]$sourceAboutMetadata = Get-Content -LiteralPath $sourceAboutPath -Raw -Encoding UTF8
if ([string]$sourceAboutMetadata.ModMetaData.packageId -ne 'zcn.qol.easyreading') {
    throw 'v1.1.0 source About.xml has unexpected packageId; no installation'
}
if ([string]$sourceAboutMetadata.ModMetaData.name -ne "QoL: Easy Reading" -or
    -not ([string]$sourceAboutMetadata.ModMetaData.description).Contains("Version: v$modVersion")) {
    throw 'v1.1.0 source About.xml version marker mismatch; no installation'
}

if (-not $BuildOnly -and [string]::IsNullOrWhiteSpace($ModRoot)) {
    throw 'Supply -ModRoot (must contain About\About.xml), or use -BuildOnly.'
}

$requiredSources = @(
    'Schematic\SchematicResearchAccess.cs',
    'Schematic\SchematicResearchState.cs',
    'Schematic\SchematicResearchResolver.cs',
    'Schematic\SchematicResearchClassifier.cs',
    'Schematic\SchematicStatusIconCatalog.cs',
    'Diagnostics\SchematicResearchAudit.cs',
    'Diagnostics\SchematicLiveResearchProbe.cs',
    'Diagnostics\SchematicIconAssetAudit.cs',
    'Settings\EasyReadingBasicTab.cs',
    'Settings\EasyReadingIconsTab.cs',
    'Settings\EasyReadingSettingsWindow.cs',
    'Books\BookContentsIconLayout.cs',
    'Schematic\SchematicBookcaseIconResolver.cs',
    'Schematic\SchematicStatusIconResolver.cs',
    'Schematic\SchematicResearchBenefitTooltip.cs',
    'Schematic\SchematicResearchSignature.cs',
    'Schematic\SchematicOwnershipResolver.cs',
    'Schematic\SchematicDuplicateTooltipContent.cs',
    'Books\SchematicTradeDuplicateTooltip.cs',
    'Schematic\SchematicOwnershipSources.cs',
    'Schematic\SchematicOwnershipTrace.cs',
    'Diagnostics\SchematicOwnershipAudit.cs',
    'Diagnostics\SchematicOwnershipTraceLogger.cs',
    'Schematic\SchematicTradeGroupResolver.cs',
    'Schematic\SchematicTradeGroupInspector.cs',
    'Schematic\SchematicTradeGroupDecisionPolicy.cs',
    'Diagnostics\SchematicTradeGroupDiagnostics.cs',
    'Diagnostics\SchematicTradeGroupRegressionAudit.cs',
    'Diagnostics\SchematicTradeGroupTestFixtures.cs',
    'Schematic\SchematicTradeOwnershipSession.cs',
    'Schematic\SchematicTradePurchaseIconPolicy.cs',
    'Schematic\SchematicTradeIconResolver.cs',
    'Schematic\SchematicTradeIconRenderer.cs',
    'Books\BookReadFloatMenuSkillIcons.cs',
    'Books\BookTradeSkillIcons.cs',
    'Books\BookTradeSkillIcons.Row.cs',
    'Books\BookTradeSkillIcons.Paint.cs',
    'Books\BookTradeSkillIcons.Textbook.cs'
)
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
# Read configuration-scoped Compile items: the Release set MUST NOT contain
# any of the diagnostic developer action sources. Fail closed on unknown groups.
$compileCommon = @($projectXml.Project.ItemGroup | Where-Object {
    $_.Compile -and -not $_.Condition
} | ForEach-Object { $_.Compile } | ForEach-Object { $_.Include })
$compileDebugOnly = @($projectXml.Project.ItemGroup | Where-Object {
    $_.Compile -and $_.Condition -and ([string]$_.Condition -like '*Debug*')
} | ForEach-Object { $_.Compile } | ForEach-Object { $_.Include })
$allCompileEntries = @($projectXml.Project.ItemGroup | Where-Object { $_.Compile } |
    ForEach-Object { $_.Compile } | ForEach-Object { $_.Include })
$debugCompile = @($compileCommon + $compileDebugOnly)
$releaseCompile = @($compileCommon)
$diagnosticOnly = @(
    'Diagnostics\SchematicResearchAudit.cs',
    'Diagnostics\SchematicLiveResearchProbe.cs',
    'Diagnostics\SchematicIconAssetAudit.cs',
    'Diagnostics\SchematicOwnershipAudit.cs',
    'Diagnostics\SchematicOwnershipTraceLogger.cs',
    'Diagnostics\SchematicTradeGroupDiagnostics.cs',
    'Diagnostics\SchematicTradeGroupRegressionAudit.cs',
    'Diagnostics\SchematicTradeGroupTestFixtures.cs'
)
if ($compileCommon.Count -ne 55 -or $compileDebugOnly.Count -ne 8 -or
    $debugCompile.Count -ne 63 -or $releaseCompile.Count -ne 55 -or
    $allCompileEntries.Count -ne 63) {
    throw "S3-REL-DIAG1 Compile count mismatch: common=$($compileCommon.Count); debugOnly=$($compileDebugOnly.Count); Debug=$($debugCompile.Count); Release=$($releaseCompile.Count)"
}
foreach ($part in $diagnosticOnly) {
    if (@($compileDebugOnly | Where-Object { $_ -eq $part }).Count -ne 1 -or
        $releaseCompile -contains $part) {
        throw "S3-REL-DIAG1 Debug-only Compile contract violated: $part"
    }
}
foreach ($required in $requiredSources) {
    if (@($allCompileEntries | Where-Object { $_ -eq $required }).Count -ne 1) {
        throw "S3-REL-DIAG1 expected exactly one Compile entry: $required"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $required) -PathType Leaf)) {
        throw "S3-REL-DIAG1 missing source: $required"
    }
}
foreach ($part in @('Core\EasyReadingRuntimeHealth.cs',
    'Settings\EasyReadingSettingsWindow.cs',
    'Books\BookTradeSkillIcons.Row.cs',
    'Books\BookTradeSkillIcons.Paint.cs',
    'Schematic\SchematicTradeOwnershipSession.cs',
    'Schematic\SchematicOwnershipTrace.cs')) {
    if (@($releaseCompile | Where-Object { $_ -eq $part }).Count -ne 1) {
        throw "S3-REL-DIAG1 missing production Compile item: $part"
    }
}
foreach ($legacy in @('Diagnostics\ReadingJobAudit.cs', 'Diagnostics\ManualReadingScheduleTrace.cs')) {
    if ($allCompileEntries -contains $legacy) {
        throw "S3-REL-DIAG1 legacy diagnostic unexpectedly compiled: $legacy"
    }
}
if (@($allCompileEntries | Select-Object -Unique).Count -ne $allCompileEntries.Count) {
    throw 'S3-REL-DIAG1 duplicate Compile items detected'
}
Write-Host "[S3-REL-DIAG1] Compile sets checked: Debug=$($debugCompile.Count), Release=$($releaseCompile.Count), diagnosticsExcludedFromRelease=8"

# Localization files are runtime assets, not C# Compile entries.
$requiredLanguages = @('Languages\English\Keyed\QoLEasyReading.xml', 'Languages\Korean\Keyed\QoLEasyReading.xml')
foreach ($relative in $requiredLanguages) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relative) -PathType Leaf)) {
        throw "S3-REL-DIAG1 language asset missing: $relative; no installation"
    }
}

$iconNames = @('Schematic_Unknown.png', 'Schematic_Review.png', 'Schematic_Complete.png', 'Schematic_DuplicateWarning.png')
$iconSources = @()
foreach ($name in $iconNames) {
    $sourcePath = Join-Path $PSScriptRoot ("Textures\QoLEasyReading\Schematic\" + $name)
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "S3-REL-DIAG1 icon missing in source: $sourcePath (no DLL/PNG installed)"
    }
    $iconSources += [pscustomobject]@{
        name = $name
        path = $sourcePath
        sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    }
}

# Files influencing the source build / localization / graphics; no bin/obj cache.
$sourceFiles = @(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File | Where-Object {
    $_.Extension -in @('.cs','.csproj','.ps1','.xml','.png') -and
    $_.FullName -notmatch '[\\/](bin|obj|S3B1_InstallBackups|S3B2_InstallBackups|S3C1_InstallBackups|S3C1L1_InstallBackups|S3C2T1_InstallBackups|S3R1_InstallBackups|S3R2_InstallBackups|S3D0HF1_InstallBackups|S3D0EFG_InstallBackups|S3RELDIAG1_InstallBackups|S3QC_HF1_InstallBackups)[\\/]'
} | Sort-Object FullName)
$sourceAudit = @($sourceFiles | ForEach-Object {
    [ordered]@{
        relativePath = $_.FullName.Substring($PSScriptRoot.Length + 1)
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
})
$manifest = [ordered]@{
    buildId = $buildId
    modVersion = $modVersion
    assemblyVersion = $expectedAssemblyVersion
    phase = 'S3QC-HF1-QualityColorsTitleSanitizer'
    originalProject = '261117 1019.zip'
    originalProjectSha256 = '8fa955f911ec09051b8a29ee69915da745811560e33e279eae7298bac4d16fe1'
    resourceCount = $iconSources.Count
    ownershipAudit = 'PRODUCTION_SNAPSHOT_SHARED;AUDITS_DEBUG_ONLY'
    settingsTabStyle = 'VANILLA_TABDRAWER_AND_MENU_SECTION'
    compileDebug = $debugCompile
    compileRelease = $releaseCompile
    devActionsExpectedDebug = 5
    devActionsExpectedRelease = 0
    settingsWindowCs = 'Settings\EasyReadingSettingsWindow.cs'
    ownershipCoverage = 'PARTIAL_FAIL_CLOSED_NEGATIVE'
    purchaseWarningIcon = 'TRADER_ONLY_OWNED'
    tooltipPolicy = 'ICON_ONLY;VERIFIED_TITLES;QC_TAG_STRIP_V1;NO_NEW_SCAN'
    sourceFiles = $sourceAudit
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$manifestSha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
Write-Host "[S3-REL-DIAG1] Sources, About.xml, 4 PNG assets and 2 Keyed XML files audited. Manifest SHA256=$manifestSha"

# Find a supported VS MSBuild (VS2022+ with .NET Framework 4.7.2 Developer Pack).
$msbuild = $null
$programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
$vswhere = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
    $candidate = & $vswhere -latest -products '*' -requires 'Microsoft.Component.MSBuild' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { $msbuild = $candidate }
}
if (-not $msbuild) {
    $fallback = @(
        (Join-Path $programFilesX86 'Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path $programFilesX86 'Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path $programFilesX86 'Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path $programFilesX86 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe')
    )
    $msbuild = $fallback | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $msbuild) { throw 'MSBuild.exe unavailable. Install VS2022 build tools + .NET Framework 4.7.2 Developer Pack.' }

$debugBuildLog = Join-Path $PSScriptRoot 'S3QC_HF1_BUILD_Debug.log'
$releaseBuildLog = Join-Path $PSScriptRoot 'S3QC_HF1_BUILD_Release.log'
Write-Host "[S3-REL-DIAG1] Rebuilding Debug and Release: $buildId"
& $msbuild $project /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU /verbosity:minimal /nologo /fl "/flp:logfile=$debugBuildLog;verbosity=normal"
if ($LASTEXITCODE -ne 0) { throw "S3-REL-DIAG1 Debug build failed: exit=$LASTEXITCODE; no installation" }
if (-not (Test-Path -LiteralPath $debugDll -PathType Leaf)) { throw "Debug DLL missing: $debugDll" }
& $msbuild $project /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /verbosity:minimal /nologo /fl "/flp:logfile=$releaseBuildLog;verbosity=normal"
if ($LASTEXITCODE -ne 0) { throw "S3-REL-DIAG1 Release build failed: exit=$LASTEXITCODE; no installation" }
if (-not (Test-Path -LiteralPath $releaseDll -PathType Leaf)) { throw "Release DLL missing: $releaseDll" }
# Prevent a previously compiled v1.0.0 DLL from being mistaken for the v1.1.0 release.
foreach ($dll in @($debugDll, $releaseDll)) {
    $actualAssemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
    $actualFileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).FileVersion
    if ($actualAssemblyVersion -ne $expectedAssemblyVersion -or
        $actualFileVersion -ne $expectedAssemblyVersion) {
        throw "Wrong version in compiled DLL: $dll; assembly=$actualAssemblyVersion; file=$actualFileVersion; expected=$expectedAssemblyVersion; no installation"
    }
}


function Test-AssemblyBuildId {
    param([string] $DllPath, [string] $ExpectedId)
    [byte[]] $data = [System.IO.File]::ReadAllBytes($DllPath)
    [byte[]] $pattern = [System.Text.Encoding]::Unicode.GetBytes($ExpectedId)
    if ($pattern.Length -eq 0 -or $data.Length -lt $pattern.Length) { return $false }
    $limit = $data.Length - $pattern.Length
    for ($offset = 0; $offset -le $limit; $offset++) {
        if ($data[$offset] -ne $pattern[0]) { continue }
        $ok = $true
        for ($i = 1; $i -lt $pattern.Length; $i++) {
            if ($data[$offset + $i] -ne $pattern[$i]) { $ok = $false; break }
        }
        if ($ok) { return $true }
    }
    return $false
}
foreach ($dll in @($debugDll, $releaseDll)) {
    if (-not (Test-AssemblyBuildId -DllPath $dll -ExpectedId $buildId)) {
        throw "Built DLL has wrong BUILD_ID: $dll (no installation)"
    }
}
# Resolve the SAME game/Harmony references used by the 1.6 project.
# The original .csproj imports ..\RimWorld.local.props; no private absolute
# paths are written into distributed source files.
if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir)) {
    $RimWorldManagedDir = $env:RimWorldManagedDir
}
if ([string]::IsNullOrWhiteSpace($HarmonyDllPath)) {
    $HarmonyDllPath = $env:HarmonyDllPath
}
$propsPath = Join-Path $PSScriptRoot '..\RimWorld.local.props'
if (Test-Path -LiteralPath $propsPath -PathType Leaf) {
    [xml]$localProps = Get-Content -LiteralPath $propsPath -Raw
    $propGroups = @($localProps.Project.PropertyGroup)
    if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir)) {
        $rawManaged = @($propGroups | ForEach-Object { $_.RimWorldManagedDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -Last 1)
        if ($rawManaged.Count -gt 0) { $RimWorldManagedDir = [string]$rawManaged[0] }
    }
    if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir)) {
        $rawRoot = @($propGroups | ForEach-Object { $_.RimWorldDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -Last 1)
        if ($rawRoot.Count -gt 0) {
            $RimWorldManagedDir = Join-Path ([string]$rawRoot[0]) 'RimWorldWin64_Data\Managed'
        }
    }
    if ([string]::IsNullOrWhiteSpace($HarmonyDllPath)) {
        $rawHarmony = @($propGroups | ForEach-Object { $_.HarmonyDllPath } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -Last 1)
        if ($rawHarmony.Count -gt 0) { $HarmonyDllPath = [string]$rawHarmony[0] }
    }
    if ([string]::IsNullOrWhiteSpace($HarmonyDllPath)) {
        $rawHarmonyDir = @($propGroups | ForEach-Object { $_.HarmonyDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -Last 1)
        if ($rawHarmonyDir.Count -gt 0) {
            $HarmonyDllPath = Join-Path ([string]$rawHarmonyDir[0]) '0Harmony.dll'
        }
    }
}
if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir) -or
    -not (Test-Path -LiteralPath (Join-Path $RimWorldManagedDir 'Assembly-CSharp.dll') -PathType Leaf)) {
    throw 'S3-REL-DIAG1: cannot resolve 1.6 Managed directory for metadata audit. Supply -RimWorldManagedDir or configure ..\RimWorld.local.props.'
}
if ([string]::IsNullOrWhiteSpace($HarmonyDllPath) -or
    -not (Test-Path -LiteralPath $HarmonyDllPath -PathType Leaf)) {
    throw 'S3-REL-DIAG1: cannot resolve Harmony DLL for metadata audit. Supply -HarmonyDllPath or configure ..\RimWorld.local.props.'
}

# CLR metadata validation runs in a fresh PowerShell process for EACH
# configuration, preventing the same-identity Debug/Release assemblies from
# being reused in one reflection-only AppDomain. Fail closed before install.
$verifier = Join-Path $PSScriptRoot 'S3RELDIAG1_Reports\VerifyDeveloperActions.ps1'
if (-not (Test-Path -LiteralPath $verifier -PathType Leaf)) {
    throw 'S3-REL-DIAG1 developer action verifier missing; no installation'
}
$psExe = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
if (-not (Test-Path -LiteralPath $psExe -PathType Leaf)) {
    $psExe = Join-Path $PSHOME 'powershell.exe'
}
if (-not (Test-Path -LiteralPath $psExe -PathType Leaf)) {
    throw 'Windows PowerShell.exe unavailable; strict metadata check required'
}
foreach ($item in @(
    [pscustomobject]@{ dll=$debugDll; expected=5; config='Debug' },
    [pscustomobject]@{ dll=$releaseDll; expected=0; config='Release' }
)) {
    & $psExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $verifier `
        -DllPath $item.dll -ExpectedCount $item.expected `
        -ManagedDir $RimWorldManagedDir -HarmonyDllPath $HarmonyDllPath
    if ($LASTEXITCODE -ne 0) {
        throw "S3-REL-DIAG1 $($item.config) DebugActionAttribute metadata audit FAILED; no installation"
    }
}
$debugHash = (Get-FileHash -LiteralPath $debugDll -Algorithm SHA256).Hash
$releaseHash = (Get-FileHash -LiteralPath $releaseDll -Algorithm SHA256).Hash
$record = [ordered]@{
    buildId = $buildId
    modVersion = $modVersion
    assemblyVersion = $expectedAssemblyVersion
    phase = 'S3-QC-HF1: QualityColorsTitleSanitizer'
    targetGame = 'RimWorld 1.6'
    ownershipAudit = 'PRODUCTION_SNAPSHOT_SHARED;AUDITS_DEBUG_ONLY'
    warningIconInstalled = $true
    translatedTooltip = $true
    builtAtLocal = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK')
    sourceManifestSha256 = $manifestSha
    debugDll = $debugDll
    debugSha256 = $debugHash
    releaseDll = $releaseDll
    releaseSha256 = $releaseHash
    debugBuildLog = $debugBuildLog
    releaseBuildLog = $releaseBuildLog
    partialFiles = 4
    compileDebug = $debugCompile.Count
    compileRelease = $releaseCompile.Count
    developerActionsDebug = 5
    developerActionsRelease = 0
    releaseDiagnostics = 'COMPILED_OUT'
    tooltipTitleSanitizer = 'QC_TAG_STRIP_V1'
    settingsWindowStyle = 'VANILLA_TABDRAWER_AND_MENU_SECTION'
    uiSourceSplit = 'EasyReadingSettingsWindow.cs; no Harmony changes'
    unchangedBehavior = 'PRODUCTION_POLICIES_UNCHANGED; 256_BUDGET_UNCHANGED; HF1_SESSION_UNCHANGED; HARMONY_TARGETS_UNCHANGED; TITLE_TRACE_PRESERVED; TITLE_OUTPUT_TAGS_STRIPPED_QC_HF1; SETTINGS_XML_UNCHANGED'
    iconSources = $iconSources
    buildVerified = $true
    installed = $false
    installEntries = @()
    backupFolder = $null
    verification = 'DEBUG_RELEASE_REBUILD_OK; BOTH_BUILD_IDS_VERIFIED; DebugActionAttribute_5_0_VERIFIED; BUILD_ONLY_NO_INSTALL'
}
if ($BuildOnly) {
    $record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding UTF8
    Write-Host "[S3-REL-DIAG1] BuildOnly: no game files installed. Record: $recordPath"
    return
}

# Validate the exact destination mod identity before writing even a temp file.
$modRootFull = [System.IO.Path]::GetFullPath($ModRoot)
$aboutPath = Join-Path $modRootFull 'About\About.xml'
if (-not (Test-Path -LiteralPath $aboutPath -PathType Leaf)) {
    throw "ModRoot is missing About\About.xml: $modRootFull; no installation"
}
[xml]$metadata = Get-Content -LiteralPath $aboutPath -Raw
if ([string]$metadata.ModMetaData.packageId -ne 'zcn.qol.easyreading') {
    throw "Wrong packageId in $aboutPath; no installation"
}
$targetDll = Join-Path $modRootFull '1.6\Assemblies\QoLEasyReading.dll'
$targetIconRoot = Join-Path $modRootFull 'Textures\QoLEasyReading\Schematic'
$entries = @()
# About.xml follows the same staging, backup, SHA-256 and rollback transaction as the DLL.
$entries += [pscustomobject]@{ name='About_XML'; source=$sourceAboutPath; destination=$aboutPath; expectedSha256=(Get-FileHash -LiteralPath $sourceAboutPath -Algorithm SHA256).Hash; existed=$false; previousSha256=$null; backup=$null; stage=$null }
$entries += [pscustomobject]@{ name='DLL'; source=$releaseDll; destination=$targetDll; expectedSha256=$releaseHash; existed=$false; previousSha256=$null; backup=$null; stage=$null }
foreach ($language in @('English','Korean')) {
    $relative = 'Languages\' + $language + '\Keyed\QoLEasyReading.xml'
    $source = Join-Path $PSScriptRoot $relative
    $destination = Join-Path $modRootFull $relative
    $entries += [pscustomobject]@{
        name = $language + '_Keyed_XML'; source = $source; destination = $destination
        expectedSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        existed = $false; previousSha256 = $null; backup = $null; stage = $null
    }
}
foreach ($icon in $iconSources) {
    $entries += [pscustomobject]@{ name=$icon.name; source=$icon.path; destination=(Join-Path $targetIconRoot $icon.name); expectedSha256=$icon.sha256; existed=$false; previousSha256=$null; backup=$null; stage=$null }
}

$backupRoot = Join-Path $PSScriptRoot ("S3QC_HF1_InstallBackups\" + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$installGuid = [guid]::NewGuid().ToString('N')
$commitStarted = $false
$rollbackStatus = 'NOT_NEEDED'
try {
    # Preflight backup and local staging for all DLL, language, and icon files before touching originals.
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    for ($i = 0; $i -lt $entries.Count; $i++) {
        $e = $entries[$i]
        $parent = Split-Path -Parent $e.destination
        if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        if (Test-Path -LiteralPath $e.destination -PathType Leaf) {
            $e.existed = $true
            $e.previousSha256 = (Get-FileHash -LiteralPath $e.destination -Algorithm SHA256).Hash
            $e.backup = Join-Path $backupRoot ("$i-" + [System.IO.Path]::GetFileName($e.destination))
            Copy-Item -LiteralPath $e.destination -Destination $e.backup -ErrorAction Stop
            if ((Get-FileHash -LiteralPath $e.backup -Algorithm SHA256).Hash -ne $e.previousSha256) {
                throw "Backup SHA mismatch for: $($e.destination)"
            }
        }
        $e.stage = "$($e.destination).s3qc-hf1-stage-$installGuid"
        Copy-Item -LiteralPath $e.source -Destination $e.stage -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $e.stage -Algorithm SHA256).Hash -ne $e.expectedSha256) {
            throw "Staging SHA mismatch for: $($e.source)"
        }
    }

    $commitStarted = $true
    foreach ($e in $entries) {
        Copy-Item -LiteralPath $e.stage -Destination $e.destination -Force -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $e.destination -Algorithm SHA256).Hash -ne $e.expectedSha256) {
            throw "Installed SHA mismatch for: $($e.destination)"
        }
    }
    $record.installed = $true
    $record.verification = 'DEBUG_RELEASE_REBUILD_OK; DebugActionAttribute_5_0_VERIFIED; BUILD_ID_MATCH; DLL_AND_ABOUT_AND_4_PNG_AND_2_XML_INSTALLED_SHA_MATCH'
    Write-Host '[S3-REL-DIAG1] DLL, About.xml, 4 PNG, and EN/KO XML installed and SHA256 verified.'
} catch {
    $originalFailure = $_.Exception.Message
    $rollbackFailures = @()
    if ($commitStarted) {
        foreach ($e in $entries) {
            try {
                if ($e.existed) {
                    Copy-Item -LiteralPath $e.backup -Destination $e.destination -Force -ErrorAction Stop
                    if ((Get-FileHash -LiteralPath $e.destination -Algorithm SHA256).Hash -ne $e.previousSha256) {
                        throw 'Restored original hash mismatch'
                    }
                } elseif (Test-Path -LiteralPath $e.destination -PathType Leaf) {
                    Remove-Item -LiteralPath $e.destination -Force -ErrorAction Stop
                }
            } catch { $rollbackFailures += "$($e.destination):$($_.Exception.Message)" }
        }
    }
    $rollbackStatus = if ($commitStarted) { if ($rollbackFailures.Count -eq 0) { 'RESTORED_ALL' } else { 'FAILED_OR_UNCERTAIN' } } else { 'NOT_NEEDED_PRECOMMIT' }
    $record.installed = $false
    $record.verification = "INSTALL_FAILED; rollback=$rollbackStatus; error=$originalFailure"
    $record.rollbackFailures = $rollbackFailures
    throw "S3-REL-DIAG1 install failed: $originalFailure; rollback=$rollbackStatus; details=$($rollbackFailures -join ', ')"
} finally {
    foreach ($e in $entries) {
        if ($e.stage -and (Test-Path -LiteralPath $e.stage -PathType Leaf)) {
            Remove-Item -LiteralPath $e.stage -Force -ErrorAction SilentlyContinue
        }
    }
    $record.backupFolder = $backupRoot
    $record.installEntries = @($entries | ForEach-Object {
        [ordered]@{ name=$_.name; destination=$_.destination; sha256=$_.expectedSha256; existedBefore=$_.existed; previousSha256=$_.previousSha256; backupPath=$_.backup }
    })
    $record.rollbackStatus = $rollbackStatus
    $record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding UTF8
}
Write-Host "[S3-REL-DIAG1] Install record: $recordPath"
Write-Host "[S3-REL-DIAG1] Previous files backed up at: $backupRoot"
