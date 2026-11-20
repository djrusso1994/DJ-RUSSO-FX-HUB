# DJ RUSSO FX HUB v0.8.12 — GitHub source

This archive contains the WPF .NET 8 project source and required resources.
Extract the contents into the repository root (alongside README.md), not inside a nested directory.
Do not delete the existing .github/workflows/main.yml or updates folder.
Do not commit bin/, obj/, .vs/, private signing certificates or locally generated publish outputs.

The existing GitHub Actions workflow will find DJRussoFXHub.csproj at the repository root and publish a Windows x64 artifact. The workflow artifact is not a GitHub Release; release publication and application auto-updates require separate configuration and validation.

The included Payload files are prebuilt binaries required by the existing .csproj.
