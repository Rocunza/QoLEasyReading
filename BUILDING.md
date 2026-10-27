# Building QoL: Easy Reading

The source projects require your **local, legally installed** RimWorld game assemblies and Harmony libraries. This repository does not bundle these dependencies.

## RimWorld 1.6 — Visual Studio 2022

1. Copy `Source/1.6/RimWorld.local.props.example` to `Source/1.6/RimWorld.local.props`.
2. Edit `RimWorldDir` to point to your 1.6 installation (contains `RimWorldWin64_Data/Managed/Assembly-CSharp.dll`).
3. Edit `HarmonyDllPath` to point to **RimWorld 1.6 Harmony 2** `0Harmony.dll`.
4. Open `Source/1.6/QoLEasyReading/QoLEasyReading.csproj`, choose **Release / Any CPU**, and build.
5. Copy the resulting `bin/Release/QoLEasyReading.dll` to `Mod/QoLEasyReading/1.6/Assemblies/` if preparing a release.

## RimWorld 1.7 — Visual Studio 2026

1. Copy `Source/1.7/RimWorld17.local.props.example` to `Source/1.7/RimWorld17.local.props`.
2. Edit `RimWorld17GameDir` (or `RimWorld17ManagedDir`) to point to the 1.7 installation containing `Assembly-CSharp.dll`, `UnityStubs.dll`, and `Waylith.dll`.
3. Edit `Harmony17DllPath` to point to the **RimWorld 1.7 Harmony 3** `0Harmony.dll`.
4. Open `Source/1.7/Projects/QoLEasyReading/QoLEasyReading.csproj`, choose **Release / Any CPU**, and build.
5. Copy the resulting `bin/Release/QoLEasyReading.dll` to `Mod/QoLEasyReading/1.7/Assemblies/` if preparing a release.

**Never commit:** `RimWorld.local.props`, `RimWorld17.local.props`, `.vs/`, `obj/`, `bin/`, `*.pdb`, unmodified game libraries, or Steam credentials.

The checked-in `Mod/` directory is a release snapshot. It includes compiled release DLLs from the uploaded build. When changing source, rebuild the appropriate version and synchronize this snapshot before distribution. The 1.7 project uses SDK default compile items, including archived diagnostics files. They are not installed as Harmony hooks by the startup code in this build, but should be reviewed before larger refactoring.
