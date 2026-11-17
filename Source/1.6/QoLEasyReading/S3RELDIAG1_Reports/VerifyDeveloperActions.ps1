# S3-REL-DIAG1: read-only reflection metadata audit of DebugActionAttribute.
# A separate PowerShell process is required for each build configuration.
param(
    [Parameter(Mandatory=$true)][string] $DllPath,
    [Parameter(Mandatory=$true)][int] $ExpectedCount,
    [Parameter(Mandatory=$true)][string] $ManagedDir,
    [Parameter(Mandatory=$true)][string] $HarmonyDllPath
)
$ErrorActionPreference = 'Stop'
$fullPath = [IO.Path]::GetFullPath($DllPath)
if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "DLL missing: $fullPath" }
if (-not (Test-Path -LiteralPath $HarmonyDllPath -PathType Leaf)) { throw "Harmony missing: $HarmonyDllPath" }
$searchDirs = @((Split-Path -Parent $fullPath), $ManagedDir,
    (Split-Path -Parent $HarmonyDllPath))
# Load ALL assemblies in the reflection-only context; no mod code executes.
$resolve = [System.ResolveEventHandler] {
    param($sender, $args)
    $name = ([Reflection.AssemblyName]::new($args.Name)).Name + '.dll'
    if ($name -ieq '0Harmony.dll') {
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom($HarmonyDllPath)
    }
    foreach ($dir in $searchDirs) {
        $candidate = Join-Path $dir $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
        }
    }
    return [Reflection.Assembly]::ReflectionOnlyLoad($args.Name)
}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolve)
try {
    $asm = [Reflection.Assembly]::ReflectionOnlyLoadFrom($fullPath)
    $flags = [Reflection.BindingFlags]::DeclaredOnly -bor
        [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor
        [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Static
    $count = 0
    $actionNames = @()
    foreach ($type in $asm.GetTypes()) {
        foreach ($method in $type.GetMethods($flags)) {
            foreach ($attribute in [Reflection.CustomAttributeData]::GetCustomAttributes($method)) {
                if ($attribute.Constructor.DeclaringType.FullName -eq 'Verse.DebugActionAttribute') {
                    $count++
                    $actionNames += $type.FullName + '::' + $method.Name
                }
            }
        }
    }
    if ($count -ne $ExpectedCount) {
        throw "Developer action metadata mismatch: expected=$ExpectedCount; actual=$count; methods=$($actionNames -join ', ')"
    }
    Write-Host "[S3-REL-DIAG1][METADATA] status=PASS; dll=$fullPath; DebugActionAttribute=$count; expected=$ExpectedCount; reflectionOnly=True"
} finally {
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolve)
}
