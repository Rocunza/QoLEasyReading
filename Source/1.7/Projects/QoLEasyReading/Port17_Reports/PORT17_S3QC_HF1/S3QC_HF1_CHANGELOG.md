# S3-QC-HF1 — RimWorld 1.7 / Quality Colors compatibility

- Target version: RimWorld 1.7, .NET 10, Harmony 3
- Official mod version: v1.1.0 (unchanged)
- Build ID: `ER-RW17-S3QC-HF1-20261117-R1`
- Exact baseline: `QoLEasyReading 261117 1202 1.7 배포(1).zip`
- Proven implementation copied from: `261117 1742 1.6.zip`

## Modified production code

`Schematic/SchematicDuplicateTooltipContent.cs`: strip recognized Unity color/bold/italic tags from ownership-traced schematic titles before duplicate aggregation and tooltip display. Preserve actual book titles, quality words, count limits, data gates and fail-closed policy. Debug-only pure audit now expects 54 checks. The imported file matches the user's verified 1.6 source byte-for-byte.

`Core/EasyReadingBuildIdentity.cs`: 1.7 build ID and `tooltipTitleSanitizer=QC_TAG_STRIP_V1` startup markers. Preserve 1.7 framework/Harmony/GUID and 1.1.0 version.

`BuildAndInstall_RW17.ps1`: update expected build ID; leave 1.7-only installer scope, 1.6 DLL preservation and Debug/Release metadata verifier unchanged.

`PORT17_SOURCE_MANIFEST.json`: refresh hashes for current source. This manifest is a source manifest and not proof of successful Windows compilation or gameplay.

`PORT17_README.md`: update versioned test procedure. The previous `Port17_Reports/PORT17_V110_RELDIAG1` reports are kept unmodified as historical evidence for the earlier build.

## Validation required on Windows

1. Rebuild Debug and Release with 1.7 reference DLLs, using `BuildAndInstall_RW17.ps1 -BuildOnly -ReferencesDir <path>`.
2. Check metadata verifier: Debug 5 / Release 0 developer actions, version 1.1.0.0, new build ID.
3. Check Debug `[S3C2T1][PURE_AUDIT] status=PASS; checks=54; failures=0` and startup markers.
4. In 1.7 with Quality Colors, hover the duplicate-schematic `!` icon; no literal `<color>` tags, actual title/quality retained.
5. Run the same action with Quality Colors disabled; also check `?` and `✓`, bookcase, right-click/read menus, Ctrl/Alt/Shift and manual Work reading.
6. Finally smoke-test the rebuilt Release DLL, copy that exact SHA-256-matched DLL for distribution.

**Not verified here:** Windows .NET 10 compile, DLL metadata of a newly built assembly, live 1.7 runtime, other mods' Harmony interaction.
