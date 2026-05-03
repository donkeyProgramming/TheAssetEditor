![AssetEditor](https://user-images.githubusercontent.com/54080240/143955132-badd843e-b823-4a4d-8326-e64be4e2c877.png)

# The Asset Editor

[![Build & Test](https://github.com/donkeyProgramming/TheAssetEditor/actions/workflows/pr-test.yml/badge.svg)](https://github.com/donkeyProgramming/TheAssetEditor/actions/workflows/pr-test.yml)
[![Latest release](https://img.shields.io/github/v/release/donkeyProgramming/TheAssetEditor)](https://github.com/donkeyProgramming/TheAssetEditor/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/donkeyProgramming/TheAssetEditor/total)](https://github.com/donkeyProgramming/TheAssetEditor/releases)

A powerful modding tool for Total War games. Provides a full suite of editors for 3D models, animations, audio, textures, and more — all in one integrated desktop application.

**[Documentation](https://donkeyprogramming.github.io/TheAssetEditor/)** · **[Video Tutorial](https://www.youtube.com/watch?v=ZlmG2AVL5-g)** · **[Latest Release](https://github.com/donkeyProgramming/TheAssetEditor/releases/latest)**

---

## Editors

### Kitbasher (3D Editor)
Full-featured 3D mesh editor for creating composite models by combining parts from multiple meshes. Supports material editing, mesh operations (merge, split, duplicate), vertex/face/object selection modes, and real-time 3D preview with a MonoGame-based viewport.

### Visual Metadata Editor
Editing interface for animation metadata. Supports manipulation of animation tags, attributes, and timing properties that control how animations blend and trigger in-game.

### Audio Editor
Comprehensive audio project editor with support for Wwise-based audio exploration, project creation, waveform visualization, format conversion, and dialogue event management.

### Animation Re-Targeting
Retargets animations between different skeletons, allowing you to transfer animations from one character model to another. Provides bone mapping and re-rigging capabilities for cross-skeleton animation reuse.

### Skeleton Editor
Visual skeleton editor for creating, editing, and rigging skeletal structures. Supports bone manipulation, weight visualization, and reference mesh display for precise rigging workflows.

### Texture Editor
Texture preview and editing tool supporting all game texture formats. Provides metadata inspection and visual inspection of textures used by game models.

### Animation Fragment Editor
Manages animation packs (.animpack files) and campaign animation bin files. Includes a batch animation exporter for bulk format conversions across your mod.

### Import/Export Editor
Handles bidirectional import/export of game asset formats, supporting GLTF/FBX to RMV2 conversions for bringing external 3D assets into the game.

### Reports
Generates analysis and diagnostic reports including material reports, mesh info dumps, metadata JSON exports, deep file searches, and dialogue event information.

---

## Getting Started

1. Download the [latest release](https://github.com/donkeyProgramming/TheAssetEditor/releases/latest)
2. Extract and run `AssetEditor.exe`
3. Load your game's pack files and start editing

For detailed guides, see the **[Documentation](https://donkeyprogramming.github.io/TheAssetEditor/)**.

## Building from Source

Prerequisites: .NET 10 SDK, Windows

```bash
git clone https://github.com/donkeyProgramming/TheAssetEditor.git
cd TheAssetEditor
dotnet build AssetEditor.sln
```

## Contributing

Pull requests are welcome. The project uses a modular architecture with dependency injection — each editor is a self-contained project under the `Editors/` folder.

## License

See the repository for license details.
