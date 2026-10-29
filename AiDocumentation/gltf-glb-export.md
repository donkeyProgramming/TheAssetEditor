# glTF/GLB Model Export

## Status

AssetEditor can export the following pack-file model sources through the existing
`Rmv_to_Gltf` exporter:

- `.rigid_model_v2`
- `.wsmodel`
- `.variantmeshdefinition`

Both `.gltf` and `.glb` outputs are supported. The implementation was audited against the
feature scope in September 2026. No release-blocking defect was found in the implemented
paths, and the full Windows solution suite, real-asset integration tests, and manual exports
pass. The branch is suitable for upstream PR preparation.

## User workflow

1. Load the pack files that contain the model and its referenced skeleton, animations,
   materials, and textures.
2. In the PackFile explorer, right-click a `.rigid_model_v2`, `.wsmodel`, or
   `.variantmeshdefinition` file.
3. Select **Export > Advanced Export**.
4. Select `Rmv_to_Gltf`.
5. Choose whether to export textures and animations. If a skeleton is available, select
   the animation clips to include. A `stand_idle` clip is selected by default when one is
   found; AssetEditor does not select the entire catalog automatically.
6. Browse for an output filename and choose either **glTF (`.gltf`)** or **Binary glTF
   (`.glb`)** in the save dialog.
7. Select **Export**.

The animation list is derived from the source model's resolved skeleton. A VMD uses the
same component traversal and shared-skeleton selection as the exporter, so the UI cannot
silently offer clips for a different component skeleton.

## Output behavior

| Source or option | Behavior |
| --- | --- |
| Plain RMV2 | Exports LOD0 geometry and the RMV2 material. If a valid sibling WSModel exists, its effective material is used. |
| WSModel | Resolves its referenced RMV2 geometry and merges its material overrides before export. |
| VMD | Exports one scene containing the first renderable/default candidate from each selected slot, including nested VMD and WSModel-backed parts. |
| `.gltf` | Writes JSON glTF and the image/buffer sidecars needed by the file. |
| `.glb` | Embeds standard glTF resources. Temporary PNGs embedded in the GLB are removed only after a successful save. |
| Warhammer mask texture | Converted to an auxiliary `*_mask.png`; it is not connected to base color because glTF has no equivalent Warhammer faction/mask channel. |
| 3D-printing outputs | Alpha-mask, raw/offset normal, and displacement images remain auxiliary sidecars. |

## Architecture

```text
RMV2 or WSModel
  -> IModelAssetResolver
  -> ResolvedModelAsset + ResolvedModelMaterial
                                  \
VMD                                -> RmvToGltfExporter
  -> IVariantMeshCompositionResolver  -> mesh/material/texture builders
  -> default renderable tree           -> shared skeleton + selected animations
                                      -> GltfSceneSaver
                                      -> .gltf or .glb
```

The resolution layer is in `GameWorld.Core` and does not construct MonoGame scene nodes or
WPF controls. This lets the viewport and exporter consume the same interpretation while
keeping future non-UI consumers possible.

### Resolution layer

| File/class | Responsibility |
| --- | --- |
| `GameWorld.Core/Services/ModelAssetResolver.cs` | Resolves RMV2/WSModel geometry, sibling WSModels, complete material mappings, effective textures, alpha state, and diagnostics. |
| `GameWorld.Core/Services/VariantMeshCompositionResolver.cs` | Parses VMDs, chooses the first renderable candidate in viewport order, recursively resolves nested definitions, preserves attachment names, and detects cycles. |
| `GameWorld.Core/Services/WsModelMaterialProvider.cs` | Compatibility adapter for older callers. New code should use `IModelAssetResolver`; the type remains present to avoid breaking consumers. |
| `Shared/GameFiles/Vmd/VariantMeshDefinition.cs` | Makes child collections safe when optional XML elements are absent. |

The renderer path (`Rmv2ModelNodeLoader`, `ComplexMeshLoader`, `KitbashSceneCreator`, and
`BmdSceneCreator`) now consumes resolved assets. This is a deliberate part of the feature:
it prevents export and viewport material rules from drifting apart.

### Export layer

| File/class | Responsibility |
| --- | --- |
| `RmvToGltfExporter.cs` | Routes direct and composed exports, flattens VMD components, creates one shared skeleton, applies attachment parenting, and emits animations once. |
| `RmvToGltfAnimationSourceResolver.cs` | Builds the UI animation catalog using the exporter's component order and skeleton-selection rule. |
| `GltfMeshBuilder.cs` / `GltfStaticMeshBuilder.cs` | Builds LOD0 primitives and standard glTF materials from effective material data. |
| `GltfTextureHandler.cs` | Converts effective texture paths, caches conversions per export, avoids basename collisions in composed scenes, and keeps Warhammer masks auxiliary. |
| `GltfSceneSaver.cs` | Lets SharpGLTF choose `.gltf` or `.glb` by extension and safely cleans embedded-GLB texture intermediates after success. |
| `RmvToGltfExporterViewModel.cs` | Initializes against the selected source, exposes animation search/selection, and passes only selected, resolvable clips to the exporter. |
| `ExporterCoreViewModel.cs` / `IExporterViewModel.cs` | Supplies the source before displaying exporter-specific options and exposes a two-format save filter. |

### Material rules

`ResolvedModelMaterial` begins with the RMV2 texture table and overlays every WSModel
texture slot, including explicit empty slots. An empty override therefore disables the
RMV2 texture instead of accidentally falling back to it. WSModel mappings must cover every
RMV2 LOD/part exactly once; otherwise resolution uses RMV2 materials and records a
diagnostic, matching the previous viewport all-or-nothing validation.

The exporter maps representable inputs to standard glTF channels: base color/diffuse,
normal, metallic-roughness/material map, specular, gloss, occlusion, and emissive. Explicit
WSModel/RMV2 alpha state takes precedence over older filename/material heuristics.

### VMD composition and attachments

For each VMD slot, inline child meshes are visited before child references, matching the
existing viewport loader. The first candidate that resolves to renderable content is used.
References are pack-root-relative, and recursive VMD cycles produce diagnostics rather than
unbounded recursion.

The first non-empty component skeleton becomes the shared glTF skeleton. Components naming
that skeleton may be skinned against it. Components with another named skeleton are
exported unskinned and produce a warning, because their joint indices cannot safely address
the shared skin. Named VMD attachment points remain eligible for bone parenting. If no
named attachment is specified, a compatible RMV matrix index may be used. A named but
unknown attachment is authoritative and does not silently fall back to a matrix index.

## Validation

### Automated coverage

Focused tests cover:

- RMV2 and explicit/sibling WSModel resolution, complete mappings, overrides, and fallback
  diagnostics;
- VMD candidate order, root-relative lookup, nested definitions, cycle detection, and a
  tracked VMD asset;
- reloadable composed GLB output;
- attachment-by-name, matrix-index attachment, invalid attachment behavior, pivot
  translation, and rigid-versus-skinned precedence;
- one shared VMD skeleton, incompatible component skeletons, and single animation emission;
- animation catalog resolution, UI initialization, default idle selection, and passing only
  selected clips;
- texture conversion reuse, basename collision avoidance, mask/base-color separation,
  DDS red/blue channel order, and successful/failed GLB cleanup;
- a full-stack Karl Franz WSModel export using the production DDS converters, including exact
  decoded RGBA preservation, mask separation, GLB reloadability, and sidecar cleanup;
- a full-stack animated Throt VMD export, including component coverage, shared joint bindings,
  animation channels, and declared attachment-bone parenting after GLB reload.

Run the full suite on Windows from the repository root:

```powershell
dotnet test .\AssetEditor.sln --configuration Release --no-restore --verbosity normal
```

Focused exporter tests:

```powershell
dotnet test .\Editors\ImportExportEditor\Test.ImportExport\Test.ImportExport.csproj --configuration Release --no-restore --filter "FullyQualifiedName~RmvToGltfExporterTests|FullyQualifiedName~GltfSceneSaverTests|FullyQualifiedName~GltfTextureExportSessionTests|FullyQualifiedName~GltfSceneAttachmentTests|FullyQualifiedName~VmdSkeletonExportTests|FullyQualifiedName~GltfAnimationCatalogTests|FullyQualifiedName~RealAssetGltfExportTests|FullyQualifiedName~TextureHelperTests"
```

Focused resolver tests:

```powershell
dotnet test .\GameWorld\GameWorldCore\GameWorld.CoreTest\GameWorld.CoreTest.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ModelAssetResolverTests|FullyQualifiedName~VariantMeshCompositionResolverTests|FullyQualifiedName~SkeletonAnimationLookUpHelperTests|FullyQualifiedName~SkeletonAnimationLookUpHelperKarlPackTests"
```

The focused Windows reruns recorded during implementation passed (GLB cleanup 5/5, resolver
11/11, the six initially failing path-dependent VMD tests 6/6, and the two real-asset export
tests 2/2). The latest full Windows solution suite also passed. Manual VMD export with several
selected animations passed. WSL is not a valid substitute for this checkout's existing
Windows restore artifacts: their NuGet assets contain the Windows-only fallback folder
`D:\Microsoft Visual Studio\Shared\NuGetPackages`.

### Tracked sample assets

- WSModel/RMV2:
  `Data/Karl_and_celestialgeneral_Pack/variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/emp_karl_franz.wsmodel`
  and its sibling `emp_karl_franz.rigid_model_v2`.
- VMD:
  `Data/Rome_Man_And_Shield_Pack/variantmeshes/_variantmodels/man/shield/celtic_oval_patterns.variantmeshdefinition`.
- Animation lookup fixture: `Data/Karl_and_celestialgeneral.pack`.
- Animated VMD integration fixture: `Data/Throt.pack`.

### Manual release matrix

For a final release candidate, open each exported file in an independent viewer such as the
Khronos glTF Sample Viewer or Blender and record the exact pack path used:

- plain RMV2 without a sibling override;
- WSModel with an obvious base-color override and multiple material differences;
- simple VMD;
- VMD containing a WSModel-backed component;
- nested VMD;
- skinned character with several selected animations;
- weapon or shield attached to an animated bone;
- both `.gltf` and `.glb` for representative textured assets.

Confirm component count, texture identity and RGB order, compatible skin bindings sharing one
ordered joint hierarchy, clip names, attachment motion, output reloadability, and expected
sidecar cleanup.

## Known limitations

- Only LOD0 is exported.
- VMD export represents one default/renderable composition, not every possible variant
  combination and not campaign/database-driven variant selection.
- Standard glTF cannot reproduce Total War shader logic such as faction tinting or all mask
  semantics. The Warhammer mask is an unbound auxiliary PNG, not a base-color or alpha map.
- Components with a skeleton name different from the chosen shared skeleton are not skinned.
- Skeletons, animations, referenced models, materials, and textures must be present in the
  currently loaded pack containers.
- The animation list is resolved synchronously when the export window opens. The lookup is
  indexed, but a very large first-time catalog may briefly delay the UI.
- Export errors still use the existing WPF `IStandardDialogs` path in `GltfSceneSaver`; the
  resolution objects are renderer/UI-independent, but the current save service is not a
  headless API.
- Animation/status diagnostics in the exporter panel are currently English strings; action
  labels are localized.

## Upstream-readiness audit

### Scope and compatibility

- The changes are confined to general AssetEditor model resolution, rendering consistency,
  import/export UI, and tests. They add no Mod Manager, Electron, Three.js, or IPC concepts.
- Existing RMV2 overloads and constructor call sites remain available. New optional/default
  interface members preserve source compatibility for existing exporter view models and test
  doubles.
- `WsModelMaterialProvider` is deprecated but retained. Its `[Obsolete]` marker is an
  intentional migration signal, not a claim that WSModel support itself is legacy.
- Nullability annotations added around optional scene parents and VMD XML collections describe
  states already accepted by the loaders; they do not change runtime signatures.
- `git diff --check` passes for the feature range, and all three edited localization JSON files
  parse successfully.

### Review risks

1. The feature range is large (47 files, roughly 4,600 additions) because renderer/exporter
   parity and VMD tests cross project boundaries. Submit it as a small stacked series rather
   than one unstructured PR.
2. The global export window is taller to accommodate animation selection. This affects every
   exporter, though minimum dimensions remain conservative. A future UI polish could make the
   content scroll or size itself per exporter.
3. `TextureHelper` corrects channel order for every DDS-to-PNG consumer, not only glTF. It has
   a focused DXT1 regression test and should be reviewed as an explicit prerequisite/fix.
4. The old private nested `GltfTextureHandler.IDDsToPngExporter` that throws
   `NotImplementedException` predates this work, is unused, and should not be mixed into these
   PRs merely as opportunistic cleanup.

### Recommended PR split

Use stacked PRs so each diff tells one story:

1. **DDS channel-order prerequisite** — the `TextureHelper` fix and regression test. This can
   also be submitted independently.
2. **WSModel-aware glTF/GLB export** — `IModelAssetResolver`, renderer parity, effective
   materials, `.glb` save selection/cleanup, and WSModel tests.
3. **Composed VMD export** — `IVariantMeshCompositionResolver`, VMD flattening, collision-safe
   texture sessions, attachments/shared skin, parser null safety, and VMD tests. Depends on 2.
4. **Animation selection UI for model exports** — shared catalog resolution, source-aware
   exporter initialization, UI controls/localization, and catalog/view-model tests. Depends on
   3 for VMD animation selection.

Before publishing, use interactive rebase to fold the path/assertion fixups into the commits
that introduced their tests and fold mask/GLB cleanup fixes into the appropriate export PR.
Do not rewrite the working branch until the PR boundaries are agreed and a backup branch or
tag exists.

## Recommended follow-ups

These are hardening items, not evidence that the validated feature is broken:

1. Run upstream CI again after history is split/rebased.
2. Translate the remaining dynamic status strings if maintainers require full localization.
3. If a headless consumer is built later, separate save-error reporting from WPF dialogs and
   expose a result/exception-based service boundary. The current resolvers can already be
   reused without MonoGame.
