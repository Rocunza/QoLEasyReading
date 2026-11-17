# Build instructions — QoL: Easy Reading v1.1.0 S3-QC-HF1

**Important:** The repository has up-to-date 1.6 and 1.7 source. The `Mod/` folder currently contains **only the 1.7 Release DLL**, because the supplied 1.6 Release DLL was the unpatched binary. The archive is not a completed dual-version installer. See [PACKAGING_STATUS.md](PACKAGING_STATUS.md).

## RimWorld 1.6 (.NET Framework 4.7.2 / Harmony 2)

1. Copy `Source/1.6/RimWorld.local.props.example` to `Source/1.6/RimWorld.local.props` and set local 1.6 `RimWorldDir` (or `RimWorldManagedDir`) and `HarmonyDllPath`. Do not commit this private props file.
2. Open `Source/1.6/QoLEasyReading/QoLEasyReading.csproj` in Visual Studio 2022 and rebuild **Debug and Release**; or, from that project directory, run:

   ```powershell
   .\BuildAndInstall_RW16.ps1 -BuildOnly
   ```

3. Check Debug 63 / Release 55 compiled C# entries, Debug 5 / Release 0 developer actions, assembly 1.1.0.0 and BUILD_ID `ER-RW16-S3QC-HF1-20261117-R1`.
4. Load **the newly rebuilt Release DLL** with Quality Colors in real RimWorld 1.6, confirm no `<color=...>` markup appears in duplicate-owned schematic tooltip and record its SHA-256.
5. Only then add that new DLL to `Mod/QoLEasyReading/1.6/Assemblies/`, restore 1.6 in the packaged About.xml supportedVersions, update `RELEASE_MANIFEST.json` and make a two-version ZIP. Do not reuse old v1.1.0 1.6 binaries.

## RimWorld 1.7 (.NET 10 / Harmony 3 / Waylith)

1. Copy `Source/1.7/RimWorld17.local.props.example` to `Source/1.7/RimWorld17.local.props` (not committed), or pass `-ReferencesDir`.
2. Provide 1.7 `Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll` and Harmony 3 `0Harmony.dll`. Do not mix 1.6/1.7 references.
3. Open `Source/1.7/Projects/QoLEasyReading/QoLEasyReading.csproj` and rebuild both configurations or run:

   ```powershell
   .\BuildAndInstall_RW17.ps1 -BuildOnly -ReferencesDir 'C:\PATH_TO_RW17_DLLS'
   ```

4. Check Debug 63 / Release 55 C# entries and Debug 5 / Release 0 developer actions; release build ID `ER-RW17-S3QC-HF1-20261117-R1`.
5. Re-test the Quality Colors duplicate tooltip **using the Release DLL**, and compare the final binary against `RELEASE_MANIFEST.json` before replacing the already-packaged approved binary.

## Release hygiene

- Official version: `v1.1.0` / assembly `1.1.0.0`; keep `QoL: Easy Reading` title and `zcn.qol.easyreading` PackageId unchanged.
- The updated QC sanitizer is intentionally identical between the 1.6 and 1.7 source projects.
- No external game, Harmony or Unity binaries are bundled. Do not commit `.git/`, `.vs/`, `bin/`, `obj/`, PDB, local `.props`, user paths, test saves or credentials.
- Do not silently promote a rebuilt DLL without metadata, hash and in-game verification.
- Confirm asset redistribution rights before publishing to GitHub Releases.
