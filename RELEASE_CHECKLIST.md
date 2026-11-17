# v1.1.0 S3-QC-HF1 release checklist

## Package/source sanity

- [x] Source/1.6 includes latest QC-hotfix source and S3-QC-HF1 RW16 build ID.
- [x] Source/1.7 includes latest QC-hotfix source and S3-QC-HF1 RW17 build ID.
- [x] 1.6/1.7 sanitizer source bytes match; no gameplay C# edits performed in repackaging.
- [x] OLD 1.6 Release DLL completely absent from repository `Mod/` and install ZIP.
- [x] Updated 1.7 Release DLL SHA-256 recorded in `RELEASE_MANIFEST.json`.
- [x] `About.xml` is valid XML; installed package declares **1.7 only**, keeps name and packageId intact; EN/KO Schematic descriptions included.
- [x] 13 textbook + 7 Schematic images and EN/KO Keyed XML copied unchanged.
- [x] No developer Debug DLL, PDB, `bin/obj`, third-party game DLL, `.git` directory or local props in ZIPs.

## Runtime checks pending

- [x] User provided 1.6 and 1.7 Debug screenshot/log evidence for QC tooltip issue resolved; 54/54 in both startup audit logs.
- [ ] Independently rebuild 1.6 **Release** and confirm `ER-RW16-S3QC-HF1-20261117-R1`, assembly 1.1.0.0 and zero DebugActions.
- [ ] Test the *newly built* 1.6 Release binary in game; check the QC markup and other reading/trade scenarios.
- [ ] Test the packaged **1.7 Release** binary in game and check zero DebugActions/Diagnostics UI, icon/tooltip and save behavior.
- [ ] Confirm installer merges cleanly after removal of previous installed copy; never reuse old 1.6 DLL.
- [ ] Verify mixed trade direction, other-mod containers and additional mod compatibility before making blanket compatibility promises.
- [ ] Maintainer confirms redistribution rights for 13 BookSkills and Schematic images.

## GitHub publication

- [ ] Copy the new repository ZIP over the local **existing** Git checkout (keep `.git/`).
- [ ] Delete the tracked stale path `Mod/QoLEasyReading/1.6/Assemblies/QoLEasyReading.dll` from the checkout. Copying the ZIP over existing files is *not* sufficient to delete it.
- [ ] Check `git status --short` and `git diff --cached` before committing.
- [ ] If releasing the current install ZIP, label it **RimWorld 1.7 only**, not as a dual 1.6+1.7 installer.
- [ ] Avoid reusing an already-published `v1.1.0` Git tag to point to different code. If it exists, use a new patch tag/version after appropriate metadata update.
