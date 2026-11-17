# QoL: Easy Reading v1.1.0 — Quality Colors hotfix (GitHub Release draft)

**This download currently installs on RimWorld 1.7 only.** Both RimWorld 1.6 and 1.7 source projects are updated, but the provided 1.6 Release DLL was built before this hotfix and is **not included**. Do not describe the current ZIP as an RW16+RW17 installer.

**Download:** `QoLEasyReading-v1.1.0-RW17-QCHF1.zip`

### Changes

- Added schematic research and duplicate-ownership status icons in reading/trade interfaces.
- Fixed literal Quality Colors markup appearing in owned schematic title tooltips, while preserving normal titles and quality text.
- Added the Basic / Icons settings tabs and maintained the vanilla normal Read/Shift behavior.
- Kept Debug-only diagnostic tools out of Release compilation.

### Installation

Extract `QoLEasyReading/` to the RimWorld **1.7** `Mods/` folder and enable Harmony first. This package has only `1.7/Assemblies/QoLEasyReading.dll`; its About.xml lists 1.7 only.

### Known constraints

1.6 users must build a fresh validated Release DLL from the included separate 1.6 source, or wait for a future dual-version artifact. Mixed/grouped trade direction and third-party containers are not exhaustively checked. Reconfirm asset redistribution rights before publishing. The provided game screenshot/log tests for QC used Debug DLLs; 1.7 Release game QA remains pending.
