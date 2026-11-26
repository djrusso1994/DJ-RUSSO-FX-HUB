# Build FX HUB from source

Open `DJRussoFXHub.slnx` with Visual Studio 2022 or later and the **.NET desktop development** workload, then build for Windows x64. Keep the bundled `Payload` files alongside the published application.

The project builds the Windows HUB and bundles the existing precompiled Payload binaries. It does not build the JUCE audio DSP. GitHub Actions creates Windows x64 build artifacts and the installer; stable releases require owner approval after testing. See [release checklist](AGENTS.md) for version and documentation requirements.

[Windows builds](https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/actions) · [Cumulative changelog](CHANGELOG.txt)
