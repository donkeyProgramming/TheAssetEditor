# PackFileTree Review — Performance & Design Issues at 1M Nodes

## CRITICAL PERFORMANCE ISSUES

### 1. `InsertChildSorted` → full re-sort + N UI notifications per insert
**File**: `PackFileTreeMutationService.cs`

`SortChildren` does `.Clear()` (fires Reset) then `.Add()` for each child (fires N times). When adding files in bulk via `OnPackFileContainerFilesAddedEvent`, each file triggers a full sort of all siblings. A directory with 10k files receiving 100 new files → 100 full sorts of 10k items.

**Fix**: Binary-search insertion or batch with a single `SetChildren` call.

---

### 2. `SetVisibilityRecursive` — 1M PropertyChanged events
**File**: `SearchFilter.cs` line 195

Sets `IsVisible` on every node even if already at the target value. With 1M nodes, both "apply filter" and "clear filter" fire 1M property notifications.

**Fix**: Add `if (node.IsVisible == visible) return;` guard.

---

### 3. `ApplyFilter` does 5 full-tree traversals per invocation
**File**: `SearchFilter.cs` line 82

1. `SetVisibilityRecursive` (all → false)
2. `MarkPathVisibleFromData` per match
3. `ApplyFoldersOnlyFilter` (all nodes)
4. `CountVisibleNodes` (all nodes)
5. `ExpandIfVisible` (all nodes)

With 1M nodes → ~5M node visits per keystroke (even with debounce, this is heavy for a UI thread).

---

### 4. `MarkPathVisibleFromData` — O(siblings) linear scan per path segment
**File**: `SearchFilter.cs` line 140

```csharp
current.Children.FirstOrDefault(c => c.Name.Equals(segmentName, ...))
```

No index on children by name. For 50k search results in a pack with directories containing 5000+ siblings, this is millions of string comparisons.

---

### 5. `OnPackFileContainerFilesAddedEvent` — per-file sort + tree navigation
**File**: `PackFileBrowserViewModel.cs` line 231

Loading a 500k-file pack triggers per-file: path parsing → tree traversal → duplicate check → full sibling re-sort. Then `Filter.Reapply()` at the end does another full pass. The *initial* `PackFileTreeBuilder` is already optimized; the mutation path is not.

---

### 6. `ObservableCollection<TreeNode> Children`
**File**: `TreeNode.cs` line 40

Every `Add`/`Remove`/`Clear` fires `CollectionChanged`. Bulk operations produce millions of events that WPF processes individually on the UI thread.

---

## IMPORTANT DESIGN ISSUES

### 7. `GetFullPath()` — O(depth) string concatenation, no caching
**File**: `TreeNode.cs` line 79

Walks to root building intermediate strings each time. Called repeatedly during lookups, drag/drop, and rename. A `StringBuilder` or cached path would help.

---

### 8. `GetAllChildFileNodes().Count` materializes a list for a count
**File**: `PackFileBrowserViewModel.cs` line 216

On root with 500k files, allocates a 500k-element list just to check if count < 200. Should use `.Take(limit+1).Count()` or a counting method with early exit.

---

### 9. `async void DebounceFilter` + thread safety
**File**: `SearchFilter.cs` line 67

`_debounceCts` is mutated without synchronization. After `await Task.Delay`, `ApplyFilter` runs on a pool thread but modifies WPF-bound properties — requires `Dispatcher.Invoke`.

---

### 10. `IDataErrorInfo` indexer as filter trigger
**File**: `SearchFilter.cs` line 17

```csharp
public string this[string columnName] => ApplyFilter(FilterText);
```

WPF validation infra calls this indexer on property changes, making it fire unpredictably. Filter logic shouldn't be driven by validation infrastructure.

---

### 11. `FindRenamedFileNode` — O(siblings × path_lookup)
**File**: `PackFileBrowserViewModel.cs` line 319

For each sibling, calls `FindPackFile` which does `GetFullPath()` + service lookup. Also fragile if multiple renames happen simultaneously.

---

### 12. Duplicate sort comparisons
**Files**: `PackFileTreeMutationService` and `PackFileTreeBuilder`

Two independently-defined `Comparison<TreeNode>` lambdas that could silently diverge, causing inconsistent sort order between initial build and runtime mutations.

---

## MINOR ISSUES

### 13. `RemoveSelf` double-clear + `.ToList()` alloc
Recursively clears children then parent also calls `.Clear()` — double work + GC pressure on large subtrees.

### 14. `CountVisibleNodes` only counts files, not folders
The auto-expand threshold may fire earlier than intended (1000 files in 1000 folders = "1000" but 2000 visible nodes).

### 15. `ForeachNode` uses recursion vs explicit stack
Inconsistent with `EnumerateAllNodesDepthFirst` which uses an explicit stack. File trees are typically shallow enough that this isn't a real issue.

### 16. `_rootNodes.ToList()` defensive copy
Allocates a defensive copy every time `ApplyFilter` runs.

---

## PRIORITY FIX ORDER

1. **Batch mutations** — `OnPackFileContainerFilesAddedEvent` should build subtrees using `PackFileTreeBuilder`'s approach (dictionary + deferred `SetChildren`) rather than per-file insert+sort
2. **Add visibility guard** — `if (node.IsVisible == visible) return;` eliminates up to 1M no-op notifications
3. **Consolidate filter traversals** — combine set-invisible + mark-visible + count + expand into fewer passes
4. **Index children by name** — `Dictionary<string, TreeNode>` on directory nodes for O(1) child lookup
