# Release Channels

## Canonical Channel

GitHub Releases is the canonical source of truth for published binaries.

Each release publishes:

- the primary mod zip
- `SHA256SUMS.txt`
- `release-metadata.json`
- release notes

## Why `release-metadata.json` Exists

Different mod sites want the same facts in different shapes:

- mod name
- version
- supported game build
- platform / store
- install path
- summary
- checksum
- license

`release-metadata.json` keeps that data in one machine-readable place so the same release can be mirrored without rewriting metadata by hand.

## GitHub

GitHub remains the canonical archive and the first publication target.

## Nexus Mods

Current recommendation:

- upload the same primary zip unchanged
- reuse the GitHub release notes as the body
- copy compatibility fields from `release-metadata.json`
- publish the same checksum value shown in `SHA256SUMS.txt`

This keeps Nexus and GitHub aligned without maintaining two different package formats.

## Future Channels

For any future mod site, keep these rules:

- one version number per release
- one canonical binary zip
- one checksum set
- one compatibility statement tied to one exact game build
- do not rename the binary package per channel unless the channel forces it
