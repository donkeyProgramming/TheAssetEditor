# AssetEditor IPC (Current Support)

This document describes the current IPC endpoint implemented by `AssetEditor`.

## Status
- Transport: Windows named pipe
- Pipe name: `TheAssetEditor.Ipc`
- Protocol: JSON line per request (`UTF-8`, newline-terminated)
- Current supported actions: `open` only

## Pipe Path (Windows)
- `\\.\pipe\TheAssetEditor.Ipc`

## Request Format
Send one JSON object followed by a newline.

### Supported action: `open`
```json
{"action":"open","path":"variantmeshes/wh_variantmodels/.../file.rigid_model_v2"}
```

### Request fields
- `action` (required): currently only `"open"`
- `path` (required): pack-internal file path to open
- `bringToFront` (optional, default `true`): bring AssetEditor window to front
- `packPathOnDisk` (optional): disk path to a `.pack` to load first if needed
- `openInExistingKitbashTab` (optional, default `false`): if `true`, and a Kitbash tab exists, import supported files into that tab instead of opening a new tab

## Open Behavior by File Type
- `.rigid_model_v2`: normal open flow (or import into existing Kitbash tab if `openInExistingKitbashTab=true`)
- `.wsmodel`: forced to open in Kitbash Editor
- `.variantmeshdefinition`: forced to open in Kitbash Editor and imported as a reference on open

## Path Handling
- Forward slashes and backslashes are accepted
- Repeated backslashes are collapsed
- Absolute paths are accepted if they contain a known pack root such as `variantmeshes\`; AssetEditor extracts the pack-relative suffix

## Pack Loading Behavior
- If `packPathOnDisk` is supplied, AssetEditor attempts to ensure that pack is available before opening `path`
- If the pack is already loaded as a standalone pack, it is not loaded again
- If the pack is already represented inside the merged `All Game Packs` container, it is not loaded again

## Response Format
AssetEditor returns one JSON response line and closes the connection.

### Success
```json
{"ok":true}
```

### Failure
```json
{"ok":false,"error":"File not found","normalizedPath":"variantmeshes\\..."}
```

## Examples
Open from already-loaded packs:
```json
{"action":"open","path":"variantmeshes/wh_variantmodels/bi1/cth/cth_great_moon_bird/cth_great_moon_bird_body_01.rigid_model_v2"}
```

Open from a mod pack on disk (auto-load if needed):
```json
{"action":"open","path":"variantmeshes/wh_variantmodels/el1/arb/arb_new_elephants/arb_base_elephant/arb_base_elephant.rigid_model_v2","packPathOnDisk":"k:/SteamLibrary/steamapps/common/Total War WARHAMMER III/data/ovn_araby.pack"}
```

Reuse an existing Kitbash tab:
```json
{"action":"open","path":"variantmeshes/wh_variantmodels/el1/arb/ane/abe/arb_base_elephant_1.wsmodel","packPathOnDisk":"k:/SteamLibrary/steamapps/common/Total War WARHAMMER III/data/ovn_araby.pack","openInExistingKitbashTab":true}
```
