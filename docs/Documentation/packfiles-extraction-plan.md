# PackFiles Extraction Plan

> Goal: Extract `Shared/SharedCore/Shared.Core/PackFiles/` into two new projects:
> - **`Shared.PackFiles.Abstractions`** (interfaces, models, enums — formerly proposed `IFileSystem`)
> - **`Shared.PackFiles`** (implementation — formerly proposed `FileSystem`)
>
> Naming note: the original request used `IFileSystem` / `FileSystem`. Those names collide
> with the existing OS-filesystem abstraction in `Shared.Core.Services` (`IFileSystemAccess`).
> Recommended names use the `.Abstractions` convention. Adjust if you prefer the originals.

## Verdict

Extraction is **possible but not trivial**. There are **5 genuine bidirectional dependencies**
between `PackFiles` and the rest of `Shared.Core`. These are legal as namespace cycles inside one
assembly, but become **illegal project-reference cycles** once split. They must be broken first.

---

## Phase 0 — Pre-flight checks

- [ ] Confirm clean build + green tests before starting (`runTests` on `Shared.CoreTest`).
- [ ] Create a branch (e.g. `refactor/extract-packfiles`).
- [ ] Decide final project names (`Shared.PackFiles` / `Shared.PackFiles.Abstractions` vs `FileSystem` / `IFileSystem`).
- [ ] Decide whether game metadata (`GameInformationDatabase`) belongs **above** or **below** pack files. This is the crux — everything else follows.

---

## Phase 1 — Break the cycles IN PLACE (no new projects yet)

Do this first so the dependency graph becomes one-directional while still in a single assembly.
Build after each move.

### 1a. PackFiles ↔ Settings (deepest cycle)
- `PFHeader.Version` returns `PackFileVersion` (defined in `Settings/GameInformationDatabase.cs`).
- `Settings/GameInformationDatabase.cs` uses `CompressionFormat` (in `PackFiles/Utility/FileCompression.cs`).
- [ ] Move enum `PackFileVersion` to a neutral/low-level location.
- [ ] Move enum `CompressionFormat` to a neutral/low-level location.
- [ ] Rebuild; fix usings.

### 1b. PackFiles ↔ Events
- `Events/Global/PackFileSavedEvent.cs` (and sibling pack events) carry `PackFile` / `IPackFileContainer` payloads.
- `Events/Global/OpenEditorCommand.cs` uses `PackFile` / `IPackFileService`.
- `PackFileService` publishes via `IGlobalEventHub`.
- [ ] Plan to relocate the pack-related event records down with the models (done in Phase 2).

### 1c. PackFiles ↔ ErrorHandling
- `ErrorHandling/PackFileLog.cs` + `CompressionInformation` use `PackFileContainer`, `FileSources`, `Utility`.
- PackFiles uses `Logging.Create`.
- [ ] Plan to move `PackFileLog` / `CompressionInformation` into the implementation project (they are pack-specific).

### 1d. PackFiles ↔ Services
- PackFiles needs `IFileSystemAccess` / `IFileSystemWatcher` (in `Services/`).
- `Services/IStandardDialogs.cs`, `FileSaveService.cs`, `TouchedFilesRecorder.cs` use PackFiles.
- [ ] Plan to move the OS-filesystem abstraction interfaces down with the abstractions project.

### 1e. PackFiles ↔ Misc
- `Misc/SaveUtility.cs` uses `IPackFileService`.
- [ ] `SaveUtility` stays up as a consumer once types are below it.

**Exit criteria for Phase 1:** dependency graph is one-directional (PackFiles no longer depends
"up" into anything that depends "down" on it). Build green.

---

## Phase 2 — Create `Shared.PackFiles.Abstractions` (interface/low-level project)

References: **only** `Shared.ByteParsing`.

Move here:
- [ ] Interfaces: `IPackFileService`, `IPackFileContainer`, `IPackFileContainerInternal`, `IDataSource`, `IDuplicateFileResolver`
- [ ] Models: `PackFile`, `PFHeader`, `PackFileContainerType` enum
- [ ] File sources: `MemorySource`, `FileSystemSource`, `PackedFileSource`
- [ ] Enums that caused the Settings cycle: `PackFileVersion`, `CompressionFormat`
- [ ] Pure helpers (no upward deps): `PathNormalization`, `PackFileSortHelper`
- [ ] OS-filesystem abstraction: `IFileSystemAccess`, `IFileSystemWatcher` (interfaces only)
- [ ] Pack event records from `Events/Global/PackFile*Event.cs`

---

## Phase 3 — Create `Shared.PackFiles` (implementation project)

References: `Shared.PackFiles.Abstractions`, `Shared.ByteParsing`.

Package references needed:
- [ ] `Microsoft.EntityFrameworkCore.Sqlite` (for `CachedPackFileContainer` / `CacheDatabase`)
- [ ] LZ4 / LZMA-SDK / ZstdSharp (compression)
- [ ] Serilog (match existing global `Using Include="Serilog"`)

Move here:
- [ ] `PackFileService`
- [ ] Containers: `PackFileContainer`, `CachedPackFileContainer`, `SystemFolderContainer`, `SystemFolderContainerFactory`
- [ ] `Serialization/`: `PackFileSerializerLoader`, `PackFileSerializerWriter`, `CacheDatabase/`
- [ ] `Utility/`: `FileCompression`, `FileEncryption`, `ManifestHelper`, `PackFileContainerLoader2`, `PackFileServiceUtility`, `PackFileUtil`
- [ ] `FileSystemAccess`, `FileSystemWatcherWrapper` (concrete OS impls)
- [ ] `PackFileLog` / `CompressionInformation` (from ErrorHandling)

---

## Phase 4 — Re-wire

- [ ] Move DI registrations from `Shared.Core/DependencyInjectionContainer.cs` into the new project(s):
  - `IPackFileService` → `PackFileService`
  - `IPackFileContainerCacheHelper`, `IPackFileContainerLoader`
  - `IFileSystemWatcher` + `Func<IFileSystemWatcher>` factory
  - `ISystemFolderContainerFactory` → `SystemFolderContainerFactory`
- [ ] Add the two new projects to `AssetEditor.sln`.
- [ ] Update `Shared.Core.csproj` and all consumer `.csproj` to reference the new projects.
- [ ] Split `Shared.CoreTest/PackFiles/` tests into a new test project (e.g. `Shared.PackFiles.Test`).

---

## Phase 5 — Cleanup & validate

- [ ] `TreatWarningsAsErrors=true` will surface stale/unused usings across the solution — fix all.
- [ ] Solution-wide namespace update for consumers of `Shared.Core.PackFiles` (broad blast radius:
      AssetEditor, every editor module, GameWorld, tests). Mostly mechanical (pure consumers).
- [ ] Run full test suite; confirm no regressions.

---

## Risk / Considerations Checklist

- [ ] **Naming collision** — `Shared.Core.Services.IFileSystemAccess` already exists; avoid `IFileSystem`/`FileSystem` ambiguity.
- [ ] **`internal` visibility** — `PackFileContainer`, `CachedPackFileContainer`, `IPackFileContainerInternal` are `internal`. If split from `PackFileService`, use `InternalsVisibleTo` or make public.
- [ ] **`GameInformationDatabase` entanglement** — single biggest decision point (game metadata ↔ pack format).
- [ ] **Package refs** — implementation project must carry EF Core Sqlite + compressors; abstractions needs only `Shared.ByteParsing`.
- [ ] **Implicit usings + warnings-as-errors** — expect a wave of build errors after moves; budget time.
- [ ] **Blast radius** — namespace change ripples solution-wide even for pure consumers.

---

## Recommended order (summary)

1. Break cycles in place (Phase 1) — keep building after each move.
2. Create `Shared.PackFiles.Abstractions` (Phase 2).
3. Create `Shared.PackFiles` (Phase 3).
4. Re-wire DI + project refs (Phase 4).
5. Cleanup + validate (Phase 5).
