## v0.8.9 — Diagnostics translations

The Diagnostics dialog now translates every status, including the five FX SHELL installation states, across all five supported UI languages. It is still a read-only check, not a validation that FL Studio successfully loads plugins.

# DJ RUSSO FX HUB v0.8.8 — NEON GHOST / Visual Studio

This archive contains the **v0.8.8 WPF/.NET 8 x64** HUB project AND one user-supplied compiled Windows x64 `DJ RUSSO FX SHELL.vst3` file in `Payload/`.

## Important architecture
The existing FX SHELL code builds **one VST3 module exposing three different effects**: Sidechain X, Reverse FX and Stutter FX. The HUB must install ONE file, not three duplicates. These plugin classes still need verification by FL Studio after installation. The 11 planned effects are not bundled or installable yet.

The uploaded compiled file embeds **version 3.0.3**. The earlier FX SHELL 3.0.2 source archive is distinct; this HUB does not compile or change JUCE DSP.

## v0.8.8 display corrections
Theme and language names remain readable in the drop-down lists and selected states. Dropdowns are more compact without using the custom WPF template previously associated with a startup exception. The header and window version are read from assembly metadata. The installer payload is unchanged.

## Build once in Windows Visual Studio
1. Extract the ENTIRE ZIP to a normal folder (not inside the ZIP).
2. Open `DJRussoFXHub.slnx` with Visual Studio 2022+ and the **.NET desktop development** workload.
3. Select **Release / x64** and build. `.NET 8` Windows Desktop SDK is required.
4. Start the resulting `DJRussoFXHub.exe`; keep its `Payload` folder alongside the EXE (Visual Studio copies it automatically).

## Install and test in FL Studio
1. Close FL Studio completely, including all running instances.
2. In HUB, click **INSTALL FX SHELL** and accept the Windows UAC administrator prompt.
3. The only destination is `C:\Program Files\Common Files\VST3\DJ RUSSO FX SHELL.vst3` (resolved from Windows CommonProgramFiles for x64).
4. The installed file must match SHA-256 `8bd68722de231324d10aa8fd767538828dac81bebc9d3bc6bc19f95884be7de5`.
5. Open FL Studio > Plugin Manager > **Find installed plugins** (enable **Verify plugins** / rescan previously verified plug-ins if needed).
6. Confirm all THREE entries: Sidechain X, Reverse FX, Stutter FX. Test open, audio, removal, closing FL Studio (especially Sidechain X stability).
7. Close FL Studio, click **UNINSTALL FX SHELL** in HUB, approve UAC, then rescan in FL Studio. All three should disappear if they came only from that shared SHELL.

## Protections and boundaries
- The installer refuses to overwrite an existing destination, even if it has the same name. **Back up your old plugin first**, and do not delete other VST3s.
- The HUB registers installation ownership in `%LOCALAPPDATA%\DJRusso\FXHub\shell-install.json` after verifying the copied file.
- Uninstallation is enabled ONLY for an installation the HUB recorded and whose content matches the bundled file; a changed or different file is never removed.
- Installation/disinstallation of VST3 directory bundles or other plugin versions is NOT implemented here.
- Updating an existing version, automatic downloads, GitHub Releases publication, signing, digital certificates, rollback, installer packaging and elevation error recovery require further testing/development.
- Windows administrator authorization is needed for the system VST3 directory.
- A Windows desktop shortcut can be created from the compiled EXE: right-click > Send to > Desktop (create shortcut). The EXE includes the FX HUB icon.
- Theme and language selections persist; settings start collapsed.

## Test status
The code has been statically checked, and the ZIP was verified. No Windows build, UAC invocation or FL Studio test was performed by the assistant. Please send the exact Visual Studio error if compilation fails, or screenshots/status after the install and uninstall trials.
