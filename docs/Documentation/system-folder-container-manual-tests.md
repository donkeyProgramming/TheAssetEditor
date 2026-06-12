# SystemFolderContainer — Manual Test Plan

Manual, in-application test cases for verifying `SystemFolderContainer` (the disk-backed,
write-through pack container that mirrors a real folder on disk and keeps the tree view in
sync via a `FileSystemWatcher`).

Two sections:

- **Part A — Functional verification.** Confirms the feature works for normal usage.
- **Part B — Bug-hunting / edge cases.** Scenarios specifically designed to expose the
  suspected bugs identified in code review (reference-identity, self-ingestion on save,
  duplicate filenames, sort ordering, threading, watcher races, destructive folder ops).

## Conventions

- **Folder under test** = the real on-disk folder you open as a `SystemFolderContainer`.
- "Open folder as pack" = the application action that creates a `SystemFolderContainer`
  from a directory (e.g. *Open Folder* / *Load Folder*).
- "In app" = perform the action through the AssetEditor UI (tree context menu, etc.).
- "On disk" = perform the action using Windows Explorer / another editor, **while the app
  is open**, to exercise the `FileSystemWatcher`.
- After an *on disk* action, allow ~1 second (300 ms debounce + processing) before checking
  the tree.
- For each test record: **PASS / FAIL**, and for FAIL capture container state, disk state,
  and tree state separately — they can diverge independently.

### Recommended seed folder

Create this structure on disk before starting, then open it as a pack:

```
TestFolder\
  readme.txt
  animations\
    skeletons\
      humanoid01.anim
  models\
    hero.rmv2
  textures\
    diffuse.dds
    normal.dds
```

---

## Part A — Functional verification

### A1. Open folder and scan
1. Open `TestFolder` as a pack.
2. **Expect:** A root node named `TestFolder` appears in the tree.
3. **Expect:** All files above are present under the correct sub-folders.
4. **Expect:** File count matches the number of real files on disk (here: 5).

### A2. Open a file from the tree
1. Double-click `textures\diffuse.dds` (or any viewable file).
2. **Expect:** It opens and shows the on-disk content (not stale/empty).

### A3. Add a file in app
1. In app, add a new file into `models\` (e.g. `unit.rmv2`).
2. **Expect (tree):** Node appears under `models\`.
3. **Expect (disk):** `TestFolder\models\unit.rmv2` exists with the written bytes.
4. **Expect:** No duplicate node, no second event.

### A4. Add a file on disk
1. On disk, create `TestFolder\scripts\campaign.lua`.
2. **Expect (tree):** `scripts\campaign.lua` appears within ~1 s.
3. **Expect (container):** `ContainsFile(@"scripts\campaign.lua")` is true (verify by opening it).

### A5. Add a folder with content on disk
1. On disk, create `TestFolder\imported\` containing `a.dds` and `b.dds` in one operation
   (e.g. copy a prepared folder in).
2. **Expect (tree):** `imported\a.dds` and `imported\b.dds` both appear.
3. **Expect:** Exactly one *files added* refresh, both files present.

### A6. Delete a file in app
1. In app, delete `textures\normal.dds`.
2. **Expect (tree):** Node removed.
3. **Expect (disk):** File removed from disk.
4. **Expect:** `textures\diffuse.dds` still present.

### A7. Delete a file on disk
1. On disk, delete `TestFolder\readme.txt`.
2. **Expect (tree):** Node removed within ~1 s.

### A8. Delete a folder in app
1. In app, delete the `animations` folder.
2. **Expect (tree + disk):** Folder and all descendants gone; siblings untouched.

### A9. Delete a folder on disk
1. On disk, delete `TestFolder\models`.
2. **Expect (tree):** `models` and its children removed within ~1 s.

### A10. Rename a file in app
1. In app, rename `textures\diffuse.dds` → `albedo.dds`.
2. **Expect (tree):** Single node now named `albedo.dds` in the same folder (no duplicate,
   no leftover `diffuse.dds`).
3. **Expect (disk):** File renamed on disk.
4. **Expect:** Opening `albedo.dds` shows the original content.

### A11. Rename a file on disk
1. On disk, rename `models\hero.rmv2` → `hero_v2.rmv2`.
2. **Expect (tree):** Old node gone, new node present within ~1 s.

### A12. Rename a folder in app
1. In app, rename `textures` → `materials`.
2. **Expect (tree + disk):** Folder renamed; all child files moved with it.

### A13. Rename a folder on disk
1. On disk, rename `animations` → `anims`.
2. **Expect (tree):** Subtree re-parented under `anims` within ~1 s.

### A14. Move a file in app
1. In app, move `models\hero.rmv2` into `textures\`.
2. **Expect (tree + disk):** File now under `textures\`, gone from `models\`.
3. **Expect:** Opening the moved file shows the original content.

### A15. Save container to .pack and reload
1. In app, save the container to `Output.pack` **outside** the folder under test.
2. **Expect:** `Output.pack` is created and is a valid pack.
3. Open `Output.pack` in a fresh AssetEditor instance.
4. **Expect:** Same file set, same folder structure, identical file contents.

### A16. Edit-and-save a file
1. Open an editable file, change it, save through the app.
2. **Expect (disk):** On-disk bytes updated.
3. **Expect:** Re-opening the file shows the new content (no stale cache).

### A17. Close container
1. Remove/close the folder pack.
2. **Expect:** Root node disappears; no crash.
3. After closing, perform an *on disk* change in that folder.
4. **Expect:** No exceptions, no ghost tree updates (watcher fully detached).

---

## Part B — Bug-hunting / edge cases

> These target specific suspected defects. A failure here is expected to be informative;
> note exactly which of container/disk/tree diverges.

> **Automation status.** The following cases are now covered by automated tests
> (and accompanying production fixes): **B1, B2, B3, B4, B5, B6, B7, B9, B10, B12, B14, B15**.
> Locations:
> - Container-level (real disk + mocked `IFileSystemWatcher`): `Shared.CoreTest/PackFiles/Models/Containers/SystemFolderContainerTests_Integration.cs` (B6, B7, B15), `...Tests_Watcher.cs` (B9-style batches, B10, B12), `...Tests_SaveToDisk.cs` (B2, B14).
> - Tree-level (real `PackFileService` + `PackFileBrowserViewModel`): `Shared.UiTest/BaseDialogs/PackFileTree/SystemFolderContainer_PackFileBrowserIntegrationTests.cs` (B1, B3, B4, B5, B9).
>
> Still **manual-only** (hard to automate deterministically):
> - **B8** — real-watcher debounce/threading under rapid bursts (timing-dependent).
> - **B11** — partially covered by `InternalAdd_DoesNotTriggerExternalEvent`; full editor round-trip stays manual.
> - **B13** — multi-GB memory behaviour.
> - **B16** — live editor UI reaction to external deletes.

### B1. Duplicate filenames in different folders (GetFullPath name fallback)
**Hypothesis:** `GetFullPath` falls back to matching by *name* when reference lookup fails,
so an operation on one of two identically-named files can resolve to the wrong path /
wrong tree node.

1. Seed `folderA\data.bin` and `folderB\data.bin` (different contents: "AAA" vs "BBB").
2. Open as pack; confirm both appear.
3. In app, **rename** `folderA\data.bin` → `data_a.bin`.
   - **Expect:** `folderA\data_a.bin` and `folderB\data.bin` exist; `folderB` untouched.
   - **Watch for:** the tree updating the *wrong* `data.bin` node, or `folderB`'s node
     losing its backing file.
4. Repeat with **move**: move `folderB\data.bin` → `folderA\`.
   - **Expect:** `folderA\data.bin` (content "BBB") and `folderA\data_a.bin` exist.
   - **Watch for:** wrong file moved, content mismatch, or a stale node.
5. Repeat with **delete**: delete one of two duplicate-named files.
   - **Expect:** Only the intended file/node is removed.

### B2. Save .pack INTO the watched folder (self-ingestion)
**Hypothesis:** `SaveToDisk` writes the temp file and does `FileMove` **without** suppressing
the watcher, so saving the pack inside its own folder makes the watcher ingest the `.pack`
into the container.

1. Open `TestFolder` as a pack.
2. Save the container to `TestFolder\TestFolder.pack` (i.e. **inside** the folder under test).
3. Wait ~2 s.
4. **Expect (correct behavior):** The new `.pack` may appear as a normal file once, and the
   container's logical contents are unchanged otherwise.
5. **Bug signature:** `TestFolder.pack` gets added to the container as a tracked file, file
   count increments unexpectedly, or saving again now includes the previous `.pack` inside
   the new one (snowballing size). Save twice and compare sizes.

### B3. Reference identity after rename (orphaned PackFile)
**Hypothesis:** `RenameFile` mutates the caller's `PackFile.Name` but stores a *new*
`PackFile` instance in the list, leaving the published instance orphaned. The tree relies on
a name/heuristic fallback to recover.

1. Rename a file in app, then immediately rename the **same** file again
   (`a.txt` → `b.txt` → `c.txt`) without any tree refresh in between.
2. **Expect:** Tree ends with a single `c.txt` node; disk has `c.txt`.
3. **Watch for:** duplicated nodes, a node that no longer opens (null backing file), or the
   second rename failing with "file not found in container".

### B4. Reference identity after move, then operate again
1. Move `models\hero.rmv2` → `textures\` in app.
2. Immediately (no refresh) rename the moved file in app.
3. **Expect:** Both operations succeed and the tree/disk agree.
4. **Watch for:** "file not found in container" on the second op (because the published
   instance differs from the stored instance).

### B5. Copy file from another pack into the folder, then re-operate
1. Open a real CA/mod pack as a source and `TestFolder` as the destination folder container.
2. Copy a file from the source into the folder container.
3. Without refreshing, rename/move/delete that just-copied file in app.
4. **Expect:** Operation targets the correct file; tree stays consistent.
5. **Watch for:** the second operation acting on the wrong file or failing to find it.

### B6. Sort-order consistency (Ordinal vs culture)
**Hypothesis:** `GetDirectoryContent` sorts with `CurrentCultureIgnoreCase` while search /
serializer use `Ordinal`. Ordering of files with mixed case, digits, and underscores can
differ between the tree, search results, and the saved pack.

1. Seed files that sort differently under ordinal vs culture rules, e.g.:
   `Zebra.txt`, `apple.txt`, `_underscore.txt`, `file1.txt`, `file10.txt`, `file2.txt`.
2. Compare the order shown in: (a) tree directory listing, (b) search results,
   (c) the saved `.pack` (reload and inspect order).
3. **Expect:** Consistent ordering across all three.
4. **Bug signature:** Underscore/case/numeric items appear in a different order in the
   directory listing than in search or in the saved pack.

### B7. Destructive DeleteFolder with empty/root input
**Hypothesis:** `DeleteFolder("")` would resolve to the root and recursively delete the
entire source folder.

1. **Backup the test folder first** (this test is destructive if the bug exists).
2. Trigger a folder delete where the folder argument could be empty/blank (e.g. attempt to
   delete the root node, or a node whose computed path is empty).
3. **Expect:** Operation is rejected or no-ops; the source folder is NOT wiped.
4. **Bug signature:** Entire `TestFolder` contents deleted from disk.

### B8. Rapid external changes (debounce + threading)
**Hypothesis:** `_fileList` is mutated on the watcher/timer thread without locking; rapid
bursts can race with UI reads or drop/duplicate events.

1. On disk, in a tight burst, create 50 files in a subfolder (script it).
2. While they are being created, scroll/expand the tree (force UI reads).
3. **Expect:** All 50 appear; no crash; no `InvalidOperationException` ("collection modified").
4. Then delete all 50 on disk in a burst.
5. **Expect:** All removed; tree consistent.

### B9. Mixed create + delete in one debounce window
1. On disk, within ~300 ms: delete one existing file AND create one new file.
2. **Expect:** Tree shows the new file and removes the deleted one; counts correct.
3. **Watch for:** one of the two changes being lost.

### B10. Duplicate / chatty watcher events
**Hypothesis:** `FileSystemWatcher` commonly raises duplicate events; the published
added/removed lists may contain duplicates.

1. Save a large file into the folder on disk (one that takes a moment to write), or copy a
   big file in so multiple `Changed`/`Created` events fire.
2. **Expect:** The file appears exactly once in the tree; no duplicate nodes.

### B11. Internal write must not echo as external
1. In app, add/rename/delete a file.
2. **Expect:** Exactly one tree update reflecting the app action — not a second update from
   the watcher seeing the same disk change (suppression working).
3. **Watch for:** double events, flicker, or a transient duplicate node.

### B12. Watcher events arriving around dispose/close
**Hypothesis:** A debounced callback can still post after `Dispose`, and suppression cleared
on dispose can let a late event misfire.

1. In app, perform a write that touches disk, then **immediately** close the container.
2. **Expect:** No exception; no ghost update after the root is gone.
3. Variation: make an on-disk change at the exact moment of closing.
4. **Expect:** Clean detach, no `NullReference`/`ObjectDisposed`.

### B13. Save very large folder (memory)
**Hypothesis:** `SaveToDisk` reads every file fully into memory (`MemorySource`) before
writing, causing a large peak.

1. Open a multi-GB folder (or many large files) as a pack.
2. Save to `.pack` while watching process memory.
3. **Expect:** Save completes without OOM.
4. **Note:** Record peak memory; a spike near the total folder size indicates the
   load-everything-into-RAM path.

### B14. Pack version of saved file
**Hypothesis:** `SaveToDisk` hardcodes `PFH5` regardless of the selected game.
1. Save the folder as a `.pack`.
2. Inspect the saved header version.
3. **Expect / confirm:** Version is `PFH5`. If a different game version was expected for the
   active game, this is the discrepancy to flag.

### B15. Path edge cases
1. Files/folders with spaces and mixed case: `My Folder\Mixed Case.TXT`.
2. Deeply nested paths (6+ levels).
3. Unicode names if your workflow allows them.
4. **Expect:** Add/rename/move/delete and save all round-trip correctly; tree paths match
   disk paths; case is normalized consistently (lookups remain case-insensitive).

### B16. External delete of a file currently open in an editor
1. Open a file from the folder in an editor tab.
2. Delete that file on disk.
3. **Expect:** Tree removes the node; the open editor degrades gracefully (no crash on next
   save/read).

---

## Results summary template

| Test | Container OK | Disk OK | Tree OK | Notes |
|------|:------------:|:-------:|:-------:|-------|
| A1   |              |         |         |       |
| ...  |              |         |         |       |
| B1   |              |         |         |       |
| ...  |              |         |         |       |

> For any FAIL, note which column diverged first — container, disk, or tree — since these
> are tracked independently and pinpoint the layer where the bug lives.
