# FX HUB release checklist

Apply this checklist to every HUB release. Do not publish a stable release until the tested build and owner approval are confirmed.

1. Update the HUB project/assembly and installer versions. Keep the existing binaries and DSP unchanged unless a tool update was explicitly approved.
2. Verify the bundled packages and SHA-256 metadata in `plugin-updates.json`. Count unique catalog effect IDs with verified bundled download metadata against the total entries in `plugins.json`. FX SHELL counts as three effects; METER X counts as one standalone tool. Do not count planned tools or double-count packages.
3. Refresh `README.md`: current HUB version, current release summary, available tools and percentage (one decimal), individual pinned downloads and feature descriptions. Keep the README focused on end users and the current stable release; older release details belong in CHANGELOG.txt, compilation instructions in BUILD.md. Identify screenshot versions honestly.
4. Update `Assets/release-badge.svg` and `Assets/suite-progress.svg` with the new release version, verified counts, percentage, accessibility labels and bar width (328 × available / total). Preserve the approved vector artwork and Neon Ghost palette.
5. Copy both updated SVG files to `docs/badges/v<VERSION>/`. Point the README image references to those new relative paths. Do not reuse the previous release image URL: versioned paths prevent old cached graphics from appearing. Keep visible plain-text current version/count/percentage next to the SVGs as a fallback.
6. Update the single cumulative `CHANGELOG.txt`; distinguish release candidates from verified stable releases. Do not claim tests that were not performed.
7. Before publishing, verify that README, both canonical SVGs, both versioned copies, project/installer versions and package catalog agree. After publishing, check the latest GitHub Release tag and installer asset and reopen the README and SVGs on main. If an image still looks stale, verify its exact versioned URL and refresh the browser.

Documentation-only corrections should not require a new software release. Keep all existing official download links and original artwork unless an update is needed.
