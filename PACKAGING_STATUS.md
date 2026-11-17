# Packaging status — S3-QC-HF1 (2026-11-17)

**Source targets:** RimWorld 1.6 and 1.7. **Binary currently packaged:** RimWorld 1.7 ONLY.

| Item | State | Evidence |
|---|---|---|
| 1.6 QC source | INCLUDED | Identical to `261117 1742 1.6.zip`; 65 C# files |
| 1.7 QC source | INCLUDED | Identical to `261117 1812 1.7.zip`; 66 C# files |
| 1.6 Release DLL | **EXCLUDED** | Latest available DLL matched PRE-QC GitHub binary (`03ff71b6168b2e827251a73b0218d00ca0ca12d9ebb37d90f9b92888124f90c4`) |
| 1.7 Release DLL | INCLUDED | Updated QC DLL SHA-256 `1b67e74594019982afcdbe3d252c254790ec2585cff8982ff9f5de49cd1451fd` |
| Both Debug UI/sanitizer tests | Observed in uploaded game logs and screenshots | 54/54 on each version, plus human-visible tooltip correction |
| Latest 1.7 Release in-game QC test | **NOT CHECKED** | Release DLL changed and build marker present, but Debug was tested in game |
| Latest 1.6 Release in-game QC test | **BLOCKED** | No current QC Release DLL supplied |
| Icon redistribution rights | **NOT CHECKED** | Maintainer action required |

The `Mod/QoLEasyReading/About/About.xml` used for this **1.7-only binary** intentionally has `supportedVersions` = 1.7. The **source project** About.xml files keep both 1.6 and 1.7 metadata for later unified packaging. This prevents the 1.7-only installer being presented as a working 1.6 download.

## Promotion to dual-version installer

1. Build 1.6 Release from the included current 1.6 source, not from any old DLL.
2. Confirm new 1.6 Release contains `ER-RW16-S3QC-HF1-20261117-R1`, assembly 1.1.0.0 and 0 developer DebugActions.
3. Test the previously broken Quality Colors duplicate tooltip in a real **1.6 Release** game.
4. Confirm the new 1.7 Release DLL likewise in game; record both DLL SHA-256 values.
5. Put the new 1.6 Release DLL in `Mod/QoLEasyReading/1.6/Assemblies/`, restore `<li>1.6</li>` in the distributed About.xml, update manifest/docs, and create a new *dual-version* ZIP.
6. **Do not** rename the current 1.7-only ZIP to `RW16-RW17`: filename and README must match its actual contents.

No Git commits, tags, pushes, or remote releases were performed during packaging.
