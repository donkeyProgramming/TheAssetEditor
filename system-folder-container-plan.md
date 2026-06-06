# SystemFolderContainer Implementation Plan

## Overview

Add a new `SystemFolderContainer` — a third implementation of `IPackFileContainerInternal` that mirrors a folder on the local file system. Changes made in the tool are written to disk; changes made on disk (add/delete/rename) are detected and reflected in the tool via existing pack-file events.

---

## Architecture Summary

```
IPackFileContainer (public)
└── IPackFileContainerInternal (internal)
    ├── PackFileContainer          — mutable, in-memory dictionary
    ├── CachedPackFileContainer    — read-only, SQLite-backed
    └── SystemFolderContainer      — mutable, file-system-backed (NEW)
```

**Key design decisions:**
- Files are represented as `PackFile` with `FileSystemSource` data sources (already exists at `Shared.Core/PackFiles/Models/FileSources/FileSystemSource.cs`).
- File-system monitoring uses `FileSystemWatcher` wrapped behind a testable `IFileSystemWatcher` interface.
- All mutations (add, delete, rename, move) write-through to disk immediately.
- `SaveToDisk` generates a `.pack` file via `PackFileSerializerWriter` but the container remains connected to the folder.
- External changes (add/delete/rename only — not content modifications) are detected, debounced (~300ms), and published via existing `PackFileContainerFilesAddedEvent` / `PackFileContainerFilesRemovedEvent` / `PackFileContainerFilesUpdatedEvent`.
- Reuse `IFileSystemAccess` for direct file I/O. Introduce a thin `IFileSystemWatcher` for monitoring.
- The container implements `IDisposable` to stop the watcher and release handles.
- `PackFileService.UnloadPackContainer` already calls `Dispose` on `IDisposable` containers (see `CachedPackFileContainer`). We follow the same pattern.

**Project locations:**
- Container class: `Shared/SharedCore/Shared.Core/PackFiles/Models/Containers/SystemFolderContainer.cs`
- Watcher abstraction: `Shared/SharedCore/Shared.Core/Services/IFileSystemWatcher.cs`
- Watcher implementation: `Shared/SharedCore/Shared.Core/Services/FileSystemWatcherWrapper.cs`
- Tests: `Shared/SharedCore/Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests*.cs`

---

## Step 1: Create `IFileSystemWatcher` Abstraction

**Goal:** Wrap `System.IO.FileSystemWatcher` behind a testable interface so unit tests can simulate disk events without touching the real file system.

**File:** `Shared/SharedCore/Shared.Core/Services/IFileSystemWatcher.cs`

```csharp
namespace Shared.Core.Services
{
    public interface IFileSystemWatcher : IDisposable
    {
        string Path { get; set; }
        bool IncludeSubdirectories { get; set; }
        bool EnableRaisingEvents { get; set; }

        event FileSystemEventHandler? Created;
        event FileSystemEventHandler? Deleted;
        event RenamedEventHandler? Renamed;
    }
}
```

**File:** `Shared/SharedCore/Shared.Core/Services/FileSystemWatcherWrapper.cs`

```csharp
namespace Shared.Core.Services
{
    public class FileSystemWatcherWrapper : IFileSystemWatcher
    {
        private readonly FileSystemWatcher _watcher;

        public FileSystemWatcherWrapper()
        {
            _watcher = new FileSystemWatcher();
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _watcher.Created += (s, e) => Created?.Invoke(s, e);
            _watcher.Deleted += (s, e) => Deleted?.Invoke(s, e);
            _watcher.Renamed += (s, e) => Renamed?.Invoke(s, e);
        }

        public string Path { get => _watcher.Path; set => _watcher.Path = value; }
        public bool IncludeSubdirectories { get => _watcher.IncludeSubdirectories; set => _watcher.IncludeSubdirectories = value; }
        public bool EnableRaisingEvents { get => _watcher.EnableRaisingEvents; set => _watcher.EnableRaisingEvents = value; }

        public event FileSystemEventHandler? Created;
        public event FileSystemEventHandler? Deleted;
        public event RenamedEventHandler? Renamed;

        public void Dispose() => _watcher.Dispose();
    }
}
```

**DI registration** (in `Shared.Core` DI container): register `IFileSystemWatcher` as transient factory.

### Unit Tests (`Shared.CoreTest/Services/FileSystemWatcherWrapperTests.cs`)

| Test Name | Validates |
|-----------|-----------|
| `Dispose_DoesNotThrow` | Wrapper disposes cleanly |
| `SetPath_PropagatesCorrectly` | Setting Path on wrapper reaches underlying watcher |
| `EnableRaisingEvents_DefaultFalse` | Events not raised until explicitly enabled |

**Verification:** Build succeeds, tests green.

---

## Step 2: Create `SystemFolderContainer` — Core Structure + Read Operations

**Goal:** Implement `IPackFileContainerInternal` backed by a real directory. Populate `FileList` by scanning the folder recursively on construction. Each `PackFile` uses `FileSystemSource`.

**File:** `Shared/SharedCore/Shared.Core/PackFiles/Models/Containers/SystemFolderContainer.cs`

**Key properties:**
```csharp
internal class SystemFolderContainer : IPackFileContainerInternal, IDisposable
{
    public string Name { get; set; }
    public bool IsCaPackFile { get; set; } = false;
    public string? SystemFilePath { get; }  // the root folder path
    
    // internal dictionary: relative-path → PackFile
    private Dictionary<string, PackFile> _fileList = new();
}
```

**Constructor:**
- Takes `string folderPath` and `IFileSystemAccess fileSystemAccess`.
- Scans `folderPath` recursively using `IFileSystemAccess.DirectoryGetFiles(folderPath, "*.*", SearchOption.AllDirectories)`.
- For each file, computes relative path (normalized via `PathNormalization`), creates `PackFile` with `FileSystemSource`.
- Sets `Name` to the folder name.
- Sets `SystemFilePath` to the absolute folder path.

**Read operations** (same logic as `PackFileContainer`):
- `GetFileCount()` → `_fileList.Count`
- `FindFile(path)` → dictionary lookup
- `ContainsFile(path)` → dictionary contains
- `GetFullPath(file)` → reverse lookup
- `GetAllFiles()` → return dictionary
- `GetAllFilesByFolder()` → group by folder prefix
- `FindAllWithExtention(ext)` → filter by extension
- `SearchFiles(filter, extensions)` → filter by name/extension
- `GetDirectoryContent(directoryPath)` → prefix match

### Unit Tests (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Read.cs`)

| Test Name | Validates |
|-----------|-----------|
| `Constructor_ScansFolder_PopulatesFileList` | File count matches disk |
| `FindFile_NormalizedPath_ReturnsPackFile` | Case-insensitive lookup works |
| `GetAllFiles_ReturnsAllScannedFiles` | Dictionary has expected count |
| `GetFullPath_ReturnsCorrectRelativePath` | Reverse lookup works |
| `ContainsFile_ExistingFile_ReturnsTrue` | Positive containment check |
| `ContainsFile_MissingFile_ReturnsFalse` | Negative containment check |
| `GetAllFilesByFolder_GroupsCorrectly` | Folder grouping is correct |
| `SearchFiles_FilterByName_ReturnsMatch` | Text filter works |
| `SearchFiles_FilterByExtension_ReturnsMatch` | Extension filter works |

**Strategy:** Use a temporary directory with known files, mock `IFileSystemAccess` for isolation.

**Verification:** Build succeeds, all tests green.

---

## Step 3: Implement Write-Through Mutations (Add, Delete, Rename, Move)

**Goal:** Mutations to the container immediately write through to the file system.

**Methods to implement:**

```csharp
public void AddOrUpdateFile(string path, PackFile file)
{
    // 1. Compute absolute path: SystemFilePath + relative path
    // 2. Ensure directory exists
    // 3. Write file bytes to disk via IFileSystemAccess.FileWriteAllBytes
    // 4. Create new FileSystemSource pointing to the absolute path
    // 5. Update _fileList dictionary
    // IMPORTANT: Temporarily disable watcher to avoid self-triggered events
}

public List<PackFile> AddFiles(List<NewPackFileEntry> newFiles)
{
    // For each entry: build path, write to disk, add to _fileList
}

public PackFile? DeleteFile(PackFile file)
{
    // 1. Find path in _fileList
    // 2. Delete from disk via File.Delete
    // 3. Remove from _fileList
}

public void DeleteFolder(string folder)
{
    // 1. Compute absolute folder path
    // 2. Delete directory recursively from disk
    // 3. Remove all matching entries from _fileList
}

public void MoveFile(PackFile file, string newFolderPath)
{
    // 1. Find old path, compute new absolute path
    // 2. Ensure target directory exists
    // 3. Move file on disk (File.Move)
    // 4. Update _fileList
    // 5. Create new FileSystemSource for the new location
}

public string RenameDirectory(string currentNodeName, string newName)
{
    // 1. Compute old and new absolute paths
    // 2. Rename directory on disk (Directory.Move)
    // 3. Update all _fileList entries with new prefix
    // 4. Recreate FileSystemSource for moved files
}

public void RenameFile(PackFile file, string newName)
{
    // 1. Find old path
    // 2. Compute new path (same directory, new name)
    // 3. Move file on disk
    // 4. Update _fileList entry
    // 5. Update file.Name and file.DataSource
}

public void SaveFileData(PackFile file, byte[] data)
{
    // 1. Find absolute path
    // 2. Write bytes to disk
    // 3. Update DataSource to new FileSystemSource (to reflect new size)
}
```

**Important:** Each mutation should temporarily suppress the file watcher (`EnableRaisingEvents = false`) around disk writes to avoid reacting to our own changes. Use a `_suppressWatcher` flag or a RAII guard.

### Unit Tests (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Write.cs`)

| Test Name | Validates |
|-----------|-----------|
| `AddOrUpdateFile_WritesFileToDisk` | File appears on disk after add |
| `AddOrUpdateFile_UpdatesFileList` | _fileList contains new entry |
| `AddFiles_MultipleFiles_AllWrittenToDisk` | Bulk add works |
| `DeleteFile_RemovesFromDiskAndFileList` | File deleted from both |
| `DeleteFile_NonExistentFile_ReturnsNull` | Graceful no-op |
| `DeleteFolder_RemovesRecursively` | All files in folder removed |
| `MoveFile_UpdatesDiskAndFileList` | File moved, old path gone, new path exists |
| `RenameFile_UpdatesDiskAndFileList` | File renamed on disk and in dictionary |
| `RenameDirectory_UpdatesAllChildPaths` | All nested paths updated |
| `SaveFileData_WritesNewContent` | Read-back matches written data |

**Strategy:** Use real temp directory (via `Path.GetTempPath()`) for write tests. Clean up in `[TearDown]`.

**Verification:** Build succeeds, all tests green.

---

## Step 4: Implement `SaveToDisk` — Pack File Generation

**Goal:** `SaveToDisk` generates a `.pack` file from the folder contents using the existing `PackFileSerializerWriter`, but the container remains active (folder is still watched).

```csharp
public void SaveToDisk(string path, bool createBackup, GameInformation gameInformation)
{
    // 1. Create backup if requested (reuse SaveUtility.CreateFileBackup)
    // 2. Open temp file stream at path + "_temp"
    // 3. Build a transient PackFileContainer from _fileList
    //    - Each file needs its data loaded into memory (ReadData from FileSystemSource)
    //    - OR pass directly to PackFileSerializerWriter if it supports IDataSource
    // 4. Call PackFileSerializerWriter.SaveToByteArray(path, tempContainer, writer, gameInformation)
    // 5. Finalize: delete original, move temp → target path
    // NOTE: Do NOT update SystemFilePath (it stays as the folder path)
}
```

**Reference:** See `PackFileContainer.SaveToDisk` for the pattern (temp file + rename).

### Unit Tests (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_SaveToDisk.cs`)

| Test Name | Validates |
|-----------|-----------|
| `SaveToDisk_GeneratesValidPackFile` | Output file can be re-loaded as PackFileContainer |
| `SaveToDisk_ContainerRemainsActive` | FileList unchanged after save, container usable |
| `SaveToDisk_CreateBackup_BackupCreated` | Old file backed up when backup=true |
| `SaveToDisk_LockedFile_ThrowsIOException` | Handles locked output path |

**Verification:** Build succeeds, all tests green, generated .pack can be loaded by the standard loader.

---

## Step 5: Implement FileSystemWatcher Integration + Debouncing

**Goal:** Detect external file system changes (created, deleted, renamed) and publish existing pack-file events so the UI tree and other consumers update automatically.

**Design:**

```csharp
// In SystemFolderContainer:
private IFileSystemWatcher _watcher;
private IGlobalEventHub _eventHub;
private Timer _debounceTimer;
private readonly List<FileSystemEventArgs> _pendingEvents = new();
private bool _suppressWatcher = false;

private void StartWatching()
{
    _watcher.Path = SystemFilePath;
    _watcher.IncludeSubdirectories = true;
    _watcher.EnableRaisingEvents = true;
    _watcher.Created += OnFileCreated;
    _watcher.Deleted += OnFileDeleted;
    _watcher.Renamed += OnFileRenamed;
}

private void OnFileCreated(object sender, FileSystemEventArgs e)
{
    if (_suppressWatcher) return;
    lock (_pendingEvents) { _pendingEvents.Add(e); }
    ResetDebounceTimer();
}

// Similar for OnFileDeleted, OnFileRenamed

private void ResetDebounceTimer()
{
    _debounceTimer?.Dispose();
    _debounceTimer = new Timer(ProcessPendingEvents, null, 300, Timeout.Infinite);
}

private void ProcessPendingEvents(object? state)
{
    List<FileSystemEventArgs> events;
    lock (_pendingEvents)
    {
        events = new List<FileSystemEventArgs>(_pendingEvents);
        _pendingEvents.Clear();
    }

    var addedFiles = new List<PackFile>();
    var removedFiles = new List<PackFile>();

    foreach (var e in events)
    {
        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                // Add to _fileList, create PackFile with FileSystemSource
                break;
            case WatcherChangeTypes.Deleted:
                // Remove from _fileList, track removed PackFile
                break;
            case WatcherChangeTypes.Renamed:
                // Remove old entry, add new entry
                break;
        }
    }

    // Publish existing events
    if (addedFiles.Count > 0)
        _eventHub.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(this, addedFiles));
    if (removedFiles.Count > 0)
        _eventHub.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(this, removedFiles));
}
```

**Watcher suppression guard:**
```csharp
private IDisposable SuppressWatcher()
{
    _suppressWatcher = true;
    return new ActionDisposable(() => _suppressWatcher = false);
}
```

### Unit Tests (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Watcher.cs`)

| Test Name | Validates |
|-----------|-----------|
| `ExternalFileCreated_PublishesFilesAddedEvent` | New file on disk → event raised |
| `ExternalFileDeleted_PublishesFilesRemovedEvent` | Deleted file → event raised |
| `ExternalFileRenamed_PublishesRemovedAndAddedEvents` | Rename → old removed + new added |
| `InternalAdd_DoesNotTriggerExternalEvent` | Write-through suppresses watcher |
| `InternalDelete_DoesNotTriggerExternalEvent` | Delete suppresses watcher |
| `MultipleRapidCreates_BatchedIntoSingleEvent` | Debouncing batches multiple events |
| `Dispose_StopsWatcher` | After Dispose, no events fire |

**Strategy:** Use mock `IFileSystemWatcher` that manually raises events. Verify via mock `IGlobalEventHub`.

**Verification:** Build succeeds, all tests green.

---

## Step 6: Integrate with `PackFileService`

**Goal:** Ensure `PackFileService` can add, unload, and manage `SystemFolderContainer` like other containers. Handle disposal on unload.

**Changes:**

1. **`PackFileService.UnloadPackContainer`** — already publishes `PackFileContainerRemovedEvent` and removes from list. Add explicit `IDisposable` check:
   ```csharp
   // After removing from list:
   if (container is IDisposable disposable)
       disposable.Dispose();
   ```
   *(Check if this is already done — CachedPackFileContainer is IDisposable.)*

2. **`PackFileService.AddContainer`** — duplicate check uses `SystemFilePath`. For `SystemFolderContainer`, `SystemFilePath` is the folder path, so duplicates are naturally prevented.

3. **No changes to `IPackFileService` interface** — `AddContainer` already accepts `IPackFileContainer`.

4. **Factory method** (optional convenience):
   ```csharp
   public IPackFileContainer LoadSystemFolder(string folderPath, bool setToMainPack = false);
   ```
   This creates a new `SystemFolderContainer`, adds it via `AddContainer`, and returns it.

### Unit Tests (`Shared.CoreTest/PackFiles/SystemFolderContainer_PackFileServiceTests.cs`)

| Test Name | Validates |
|-----------|-----------|
| `AddContainer_SystemFolder_RegistersSuccessfully` | Container added to list, event published |
| `AddContainer_DuplicateFolderPath_Rejected` | Same folder path rejects second load |
| `UnloadContainer_DisposesWatcher` | Unloading disposes the container |
| `SetEditablePack_SystemFolder_Works` | Can mark as editable |
| `AddFilesToPack_SystemFolder_WritesThrough` | Files written to disk via service |
| `DeleteFile_SystemFolder_DeletesFromDisk` | File removed from disk via service |
| `SavePackContainer_SystemFolder_GeneratesPackFile` | Pack file generated on disk |
| `FindFile_AcrossContainers_FindsInSystemFolder` | Global search finds files in folder container |

**Verification:** Build succeeds, all tests green.

---

## Step 7: Disposal & Handle Cleanup

**Goal:** Ensure no dangling handles when container is unloaded or app shuts down.

**Cleanup responsibilities:**
1. `IFileSystemWatcher.Dispose()` — stops watching, releases OS handles.
2. `Timer.Dispose()` — stops debounce timer.
3. `PackFileService` unload path calls `Dispose`.
4. Application shutdown (`ForceShutdownEvent`) should unload all containers (already handled by existing shutdown flow).

**Implementation in `SystemFolderContainer.Dispose()`:**
```csharp
public void Dispose()
{
    _watcher.EnableRaisingEvents = false;
    _watcher.Dispose();
    _debounceTimer?.Dispose();
    _fileList.Clear();
}
```

**Check `PackFileService.UnloadPackContainer`:**
```csharp
// Existing code removes from list. Ensure IDisposable.Dispose is called:
_packFileContainers.Remove(container);
(container as IDisposable)?.Dispose();
```

### Unit Tests (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Dispose.cs`)

| Test Name | Validates |
|-----------|-----------|
| `Dispose_StopsRaisingEvents` | EnableRaisingEvents set to false |
| `Dispose_ClearsFileList` | No lingering references |
| `Dispose_CalledTwice_NoException` | Idempotent disposal |
| `UnloadPackContainer_CallsDispose` | Service calls Dispose on SystemFolderContainer |

**Verification:** Build succeeds, all tests green, no resource leaks.

---

## Step 8: DI Registration & Integration Test

**Goal:** Register new types in DI container and create an integration test that exercises the full workflow.

**DI registration** (in `Shared.Core` DI container — likely `DependencyInjectionContainer.cs` under SharedCore):
```csharp
services.AddTransient<IFileSystemWatcher, FileSystemWatcherWrapper>();
// SystemFolderContainer is created via factory, not directly resolved
```

**Integration test** (`Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Integration.cs`):

| Test Name | Validates |
|-----------|-----------|
| `FullWorkflow_CreateFolder_AddFile_DeleteFile_Save` | End-to-end: create container from temp dir, add file via service, verify on disk, delete via service, verify removed, save as pack |
| `FullWorkflow_ExternalAdd_DetectedAndEventsPublished` | Create real watcher, drop file into dir, verify event fires |

**Verification:** Full build succeeds, all tests green.

---

## File Inventory

| File | Purpose | New/Modified |
|------|---------|------|
| `Shared.Core/Services/IFileSystemWatcher.cs` | Watcher abstraction | New |
| `Shared.Core/Services/FileSystemWatcherWrapper.cs` | Real watcher impl | New |
| `Shared.Core/PackFiles/Models/Containers/SystemFolderContainer.cs` | Main container | New |
| `Shared.Core/PackFiles/PackFileService.cs` | Dispose on unload, optional factory | Modified |
| `Shared.Core/DependencyInjection/...` | Register new services | Modified |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Read.cs` | Read operation tests | New |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Write.cs` | Write operation tests | New |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_SaveToDisk.cs` | Pack generation tests | New |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Watcher.cs` | Watcher event tests | New |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Dispose.cs` | Cleanup tests | New |
| `Shared.CoreTest/PackFiles/SystemFolderContainer_PackFileServiceTests.cs` | Service integration tests | New |
| `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Integration.cs` | Full workflow tests | New |
| `Shared.CoreTest/Services/FileSystemWatcherWrapperTests.cs` | Watcher wrapper tests | New |

---

## Existing Code to Reference

| Class / File | Why |
|---|---|
| `PackFileContainer` (`Shared.Core/PackFiles/Models/Containers/PackFileContainer.cs`) | Base pattern for dictionary-based container, read/write methods |
| `CachedPackFileContainer` (same folder) | IDisposable pattern, read-only throws |
| `FileSystemSource` (`Shared.Core/PackFiles/Models/FileSources/FileSystemSource.cs`) | Data source for disk files |
| `IFileSystemAccess` / `FileSystemAccess` (`Shared.Core/Services/`) | Testable file I/O wrapper |
| `PackFileService` (`Shared.Core/PackFiles/PackFileService.cs`) | Container lifecycle, event publishing |
| `PackFileSavedEvent.cs` (`Shared.Core/Events/Global/`) | All existing pack-file events |
| `PackFileSerializerWriter` (`Shared.Core/PackFiles/Serialization/`) | .pack file generation |
| `PathNormalization` (`Shared.Core/PackFiles/Utility/`) | Path normalization rules |
| `PackFileContainerTests_TestBase` (`Shared.CoreTest/PackFiles/Models/Containers/`) | Test pattern for containers |

---

## Execution Order & Dependencies

```
Step 1 ──► Step 2 ──► Step 3 ──► Step 4
                 │                   │
                 └──► Step 5 ◄──────┘
                           │
                           ▼
                      Step 6 ──► Step 7 ──► Step 8
```

- Steps 1–3 are strictly sequential (each builds on previous).
- Step 4 (SaveToDisk) depends on Step 3 (write-through established).
- Step 5 (Watcher) depends on Steps 1 + 2 (watcher interface and file list).
- Step 6 (Service integration) depends on Steps 3 + 5.
- Step 7 (Disposal) depends on Steps 5 + 6.
- Step 8 (DI + Integration) depends on all previous steps.

---

## Notes

- **Thread safety:** FileSystemWatcher callbacks come on thread-pool threads. `_pendingEvents` list is locked. Event publishing should marshal to UI thread if needed (check how `PackFileBrowserViewModel` handles this — it likely dispatches to `Application.Current.Dispatcher`).
- **Path separator:** The pack-file system uses `\` as separator. `SystemFolderContainer` should normalize all paths via `PathNormalization.NormalizeFileName` (which lowercases and converts `/` to `\`).
- **IsCaPackFile:** Always `false` for `SystemFolderContainer`. This allows mutations in `PackFileService`.
- **Empty directories:** The internal dictionary only tracks files, not empty directories. Empty directories on disk are ignored (same as PackFileContainer behavior).
- **Large folders:** Initial scan may be slow for very large directories. Consider async loading if needed (out of scope for initial implementation).
