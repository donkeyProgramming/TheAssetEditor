# Packfile Handling Technical Documentation

## Purpose
This document describes how packfile handling works across Shared.Core, Shared.Ui, and AssetEditor UI command entry points.
It is intended for mixed audiences: contributors, maintainers, and advanced QA.

## Scope
Included:
- Packfile container model and container types
- Load, edit, and save flows
- System folder project behavior
- Caching and read-only game pack behavior
- UI integration with the pack tree and context menu commands
- Event flow and unsaved-state tracking

Not included:
- Full binary format specification for all Total War pack variants
- Deep cache database schema internals beyond architecture-level

## High-level Architecture
Main layers:
- Shared.Core.PackFiles: data model, service orchestration, loading, serialization, and container implementations
- Shared.Ui PackFileTree: tree rendering, context-menu actions, drag/drop, unsaved-state markers
- AssetEditor commands/view models: user entry points (open/create/import/set editable/save)

Core orchestrator:
- `PackFileService`

Core loaders/serializers:
- `PackFileContainerLoader`
- `PackFileSerializerLoader`
- `PackFileSerializerWriter`

Core container types:
- `PackFileContainer` (normal in-memory editable/read-only pack)
- `CachedPackFileContainer` (database-backed read-only container for game packs)
- `SystemFolderContainer` (folder-backed editable project)

## Core Data Model
### PackFile
`PackFile` wraps:
- `Name`
- `DataSource` (`IDataSource` implementation)

Factory helpers:
- in-memory bytes/ascii
- filesystem source

### Data sources
Common source types:
- `MemorySource`: mutable in-memory bytes
- `FileSystemSource`: points to a file on disk
- `PackedFileSource`: points to a range inside an on-disk .pack file

### PackFileSettings
Per-container mutable settings object:
- `SaveLocationPath`
- `GameVersion`
- `IgnoredFilesWhenSerializing`
- `SettingsChanged` event (including collection changes)

### Header model
`PFHeader` is kept on normal pack containers and used by serializer load/write.

## Container Contract
Public read contract (`IPackFileContainer`):
- query files, path lookup, search, metadata

Internal write contract (`IPackFileContainerInternal`):
- add/delete/move/rename/save file content/save container

`PackFileService` casts to internal contract and enforces policy:
- prevents edits on read-only containers
- publishes domain events
- manages editable pack selection

## Container Types and Intended Usage
### PackFileContainer (Normal)
Use for:
- loaded individual packs
- new writable output packs

Behavior:
- keeps file table in memory (`Dictionary<string, PackFile>`)
- supports full edit operations
- save writes a temp file then replaces target
- save uses settings `GameVersion` if set
- serializer ignores files listed in settings ignore list

### CachedPackFileContainer (Database)
Use for:
- large read-only game-pack collections

Behavior:
- always read-only
- file metadata stored in SQLite cache
- lookup operations query DB
- supports game-pack loading with cached fingerprint validation

### SystemFolderContainer (System Folder project)
Use for:
- folder-as-project workflows

Behavior:
- tracks files from real folder tree
- add/edit/delete/rename/move mutate disk files
- filesystem watcher tracks external created/deleted/renamed files (debounced)
- save builds a transient pack container and serializes to .pack
- persists project settings in `project_ignore.json`
- auto-excludes `project_ignore.json` from tracked pack content

## Load Flows
### Open game packs (read-only, usually at startup)
Entry points:
- App startup auto-load (if setting enabled)
- `OpenGamePackCommand`

Flow:
1. Resolve game path from settings
2. Load manifest or scan folder
3. Build merged container (often database cached)
4. Mark as CA pack / read-only
5. Add to `PackFileService`

### Open individual .pack
Entry points:
- recent pack items in `MenuBarViewModel`
- import reference pack command (read-only)

Flow:
1. `PackFileContainerLoader.CreateFromPackFile`
2. `PackFileSerializerLoader.Load` reads header/index entries
3. Create `PackFileContainer` with `PackedFileSource` entries
4. Add container to service

### Open system folder project
Entry points:
- `OpenProjectCommand`
- `CreateNewProjectCommand`
- `ImportPackAsAsProjectCommand` (extract pack then open folder)

Flow:
1. Create `SystemFolderContainer`
2. Load `project_ignore.json` if present
3. Ensure project settings file remains ignored
4. Set fallback game version from app settings when missing
5. Add container and set editable

## Editable Pack Model
One container can be marked as editable at a time.
- Managed by `PackFileService.SetEditablePack`
- UI marks this root as main/editable
- Edit commands target this pack or selected pack container as appropriate

Read-only containers cannot be set editable.

## Edit Operations
### Add/import
- `AddFilesToPack` accepts `NewPackFileEntry`
- Import file/dir context menu reads system files into memory source and adds to container

### Copy from one pack to editable pack
- `CopyToEditablePackCommand` walks selected node file descendants
- service copies each file path via `CopyFileFromOtherPackFile`

### Move/rename/delete
- service methods call container methods and publish before/after events
- drag/drop in tree uses `DropHandler` and calls `MoveFile`

### Save file bytes
- editor save operations use `SaveFile` on service for currently editable file

## Save Flows
### Save
`SavePackFileContainerCommand` behavior:
- if container has no `SystemFilePath`, prompt save path
- for `SystemFolderContainer`, force `.pack` extension
- call `PackFileService.SavePackContainer`

### Save As
`SaveAsPackFileContainerCommand` behavior:
- always prompt save path
- save selected container to path
- clear unsaved markers for root

### Serializer behavior
`PackFileSerializerWriter.SaveToByteArray`:
- normalizes ignore list entries via `PathNormalization.NormalizeFileName`
- filters out ignored file keys
- sorts by pack path comparer
- writes header and file table/blob

## Path Normalization Semantics
Current `NormalizeFileName` behavior:
- replace `/` with `\`
- lowercase
- trim whitespace

It does not canonicalize duplicate separators or remove leading separators.
This matters for ignore-path matching because serializer uses exact key membership after normalization.

## System Folder Project Settings
Stored in project root as `project_ignore.json`.
Current fields:
- `GameVersion`
- `IgnoredFilesWhenSerializing`

Persistence behavior:
- loaded at container creation
- saved whenever settings change
- `project_ignore.json` itself is always enforced in ignore list
- watcher scan and event handlers skip project settings file

## Event Flow and UI Sync
`PackFileService` publishes events for:
- container add/remove/editable-set
- files added/removed/updated
- folder removed/renamed
- container saved

`PackFileBrowserViewModel` subscribes and updates tree nodes:
- adds/removes/renames nodes
- maintains unsaved change tracker
- refreshes filter projections

## Unsaved Change Tracking
Tree roots use `UnsavedChangesTracker`.
- File/folder edits mark changed paths and ancestors
- Save event clears root unsaved state

## Context Menu Surface (Main application)
Pack operations:
- Close
- Set as Editable Pack
- Save
- Save As
- Copy to editable pack

Folder operations:
- Import file
- Import directory
- Create folder
- Rename
- Delete

File operations:
- Duplicate
- Rename
- Delete
- Copy node path

Other:
- Export to system folder
- Expand/collapse
- Open pack in file explorer
- Open in HxD / Notepad++

## Error/Recovery Behavior
Common protections:
- read-only guard for edit operations
- duplicate container load prevention by normalized system path
- save blocked when target path or temp path is locked
- optional backup creation in save utilities
- load path checks for missing game directory and missing files

## Testing Pointers
High-value integration tests are in:
- `SystemFolderContainerTests_Integration`

Important scenarios covered include:
- add/delete/rename/move/save flows
- external watcher event handling
- save ordering consistency
- ignore-list save filtering
- game version override behavior
- system folder project settings persistence
- lock recovery behavior during save

## Image Placeholders (to be added later)
1. Placeholder: architecture overview diagram
- Suggested image: service + container + serializer + UI event flow

2. Placeholder: save pipeline diagram
- Suggested image: editable container -> serializer -> temp file -> final path replacement

3. Placeholder: system folder project lifecycle
- Suggested image: folder scan + watcher + project_ignore.json + save to pack
