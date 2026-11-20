# Update feed format

The file `updates/manifest.json` is the public release index. It currently contains **no active releases**.

## Publication requirements
1. Build and test the HUB and plugin package on Windows.
2. Upload the approved package to a versioned GitHub Release.
3. Calculate the SHA-256 checksum of the exact downloadable asset.
4. Update the manifest only after the Release asset is publicly accessible.
5. Test download, checksum verification, installation with DAW closed, and rollback.

## Proposed entry shape (documentation only)
```json
{
  "id": "dj-russo-sidechain-x",
  "name": "DJ RUSSO SIDECHAIN X",
  "version": "2.3.0",
  "download_url": "https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/releases/download/<tag>/<asset>.zip",
  "sha256": "<64 lowercase hexadecimal characters>",
  "min_hub_version": "0.9.0",
  "release_notes": "Description of changes"
}
```

This is a proposed contract, **not yet supported by the desktop HUB**. Do not publish release entries until the updater implementation and its compatibility contract are tested.

## Security
- HTTPS only; downloads must be from the official release repository.
- Verify checksums before extraction; reject ZIP path traversal and unexpected files.
- Show version and notes, ask for confirmation, and never overwrite a VST3 used by a running DAW.
- Keep backup/rollback, preserve VST3 identifiers and parameter/preset compatibility.
- GitHub repository files are public: do not commit private source, tokens, passwords or signing keys.
