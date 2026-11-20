<div align="center">

<img src="Assets/hub.png" alt="DJ RUSSO FX HUB logo" width="140" />

# DJ RUSSO FX HUB
### NEON GHOST · Audio Tools Manager for Windows

**Manage DJ RUSSO audio tools in one place.**

[**Download latest public version**](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/releases) · [**Windows builds**](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/actions) · [**Report an issue**](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/issues)

![Windows](https://img.shields.io/badge/Windows-x64-39D8ED) ![Framework](https://img.shields.io/badge/.NET-8-9B5DE5) ![Status](https://img.shields.io/badge/v0.8.14-testing-BA68FF)

</div>

---

## Interface preview

The **Neon Ghost** interface provides a searchable plugin catalog, installed-software detection, language and theme settings, and a dedicated product information panel.

> **Screenshots:** Real interface screenshots from the v0.8.13 preview are being prepared for this gallery. They will be added here after the image files are uploaded to the repository.

| Dashboard | Installed audio tools |
|:---:|:---:|
| Product catalog and quick actions | Sidechain X, Reverse FX, Stutter FX and METER X |

| METER X monitoring | Interface customization |
|:---:|:---:|
| Audio analysis and monitoring | Neon Ghost and Dark Professional themes |

## Included tools

- **DJ RUSSO METER X** — Windows audio monitoring application.
- **DJ RUSSO FX SHELL** — a single VST3 module providing **Sidechain X**, **Reverse FX**, and **Stutter FX**.
- **More audio tools** — shown as *Coming Soon*; not yet available for installation.

The FX HUB itself is a Windows application, **not a VST3 plugin**.

## Features

- Install and remove supported DJ RUSSO tools.
- View installation status and launch supported applications.
- Search and filter the audio-tool catalog.
- Choose interface language and visual theme.
- Check GitHub Releases for newer versions, including preview releases.
- Open diagnostics, backup tools and the VST3 directory.
- Neon Ghost visual design and mini chip player.

## Download and install

1. Open [GitHub Releases](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/releases).
2. Download the Windows x64 **Setup .exe** from the latest published release.
3. Run the installer and launch **DJ RUSSO FX HUB**.
4. Close your DAW before installing or removing VST3 effects.
5. Open FL Studio's Plugin Manager and rescan plugins after installation.

**Current development build:** v0.8.14 — awaiting Windows testing. The newest public download may still be v0.8.13 until this build is approved.

## Compatibility and important notes

- Windows x64; developed with WPF and .NET 8.
- FL Studio is the primary testing DAW; other VST3 hosts are not yet fully validated.
- FX SHELL contains three effects in **one shared VST3 file**. Do not manually split or duplicate it.
- Installation may require Windows administrator permission.
- Always back up important projects and plugins before testing.
- The update checker **notifies and opens GitHub Releases**; it does not silently download or install software.
- The source project builds the HUB and bundles precompiled payloads. It does **not** build the JUCE audio DSP.

## Development and release process

Changes to the `main` branch trigger a Windows test build through GitHub Actions. **Public stable releases require approval after testing.** Build artifacts and public Releases are separate download channels.

### Current changes under test (v0.8.14)

- Correct GitHub update checking when only pre-releases exist (fixes the 404 response).
- Fit long product titles inside the right-side details panel, including Vocal Remover X.
- Refresh documentation and prepare the visual gallery.

These changes have been committed to source; successful compilation and Windows behavior must still be verified.

## Build from source

Open `DJRussoFXHub.slnx` with Visual Studio 2022 or later and the **.NET desktop development** workload, then build for Windows x64. Keep the bundled `Payload` files alongside the published application.

---

<div align="center">

**Created by DJ RUSSO** · [Official downloads](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/releases)

</div>
