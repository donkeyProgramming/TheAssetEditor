using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Csc;
using Shared.GameFormats.Esf;

namespace Editors.CscEditor.Data
{
    public enum CscManifestKind
    {
        /// <summary>No composite-scene tail at all (scenes with no keyframe manifest).</summary>
        None,
        /// <summary>Tail is a COMPOSITE_SCENE record (the modern, common form).</summary>
        WrappedRecord,
        /// <summary>Tail is the same payload inlined without a record wrapper (old ROOT v1-v3 files).</summary>
        Inline,
    }

    /// <summary>One entry of COMPOSITE_SCENE's entry section, kept raw plus its decoded element ids.</summary>
    public class CscManifestEntry
    {
        public List<EsfNode> Fields { get; set; } = [];
        public List<int> ElementIds { get; set; } = [];
    }

    /// <summary>
    /// A decoded .csc scene: ROOT's header, the element list, the parent/child attach tree and
    /// COMPOSITE_SCENE's manifest, all editable, with every not-understood field preserved as raw
    /// ESF nodes so a save round-trips everything the editor doesn't model.
    /// </summary>
    public class CscScene
    {
        public EsfDocument Document { get; private set; } = null!;
        public byte RootVersion { get; private set; }

        /// <summary>ROOT's version-dependent header fields, preserved verbatim (field 0 is the scene duration).</summary>
        public List<EsfNode> HeaderFields { get; private set; } = [];

        /// <summary>All elements in ROOT encounter order (the order element groups are written back in).</summary>
        public List<CscElement> Elements { get; } = [];

        /// <summary>Top-level elements (attach-tree children of -1), in attach order.</summary>
        public List<CscElement> RootElements { get; } = [];

        // ---- Composite-scene tail ----
        public CscManifestKind ManifestKind { get; private set; } = CscManifestKind.None;
        public bool ManifestParsed { get; private set; }
        public string CompositeSceneRecordName { get; private set; } = "COMPOSITE_SCENE";
        public byte CompositeSceneVersion { get; private set; }
        public EsfRecordFlags CompositeSceneFlags { get; private set; }
        public List<CscManifestEntry> ManifestEntries { get; } = [];
        public List<EsfNode> ManifestLeftoverFields { get; private set; } = [];

        /// <summary>Tail fields (everything after the attach section) preserved raw; when the tail
        /// contains a parsed COMPOSITE_SCENE record, <see cref="TailSceneRecordIndex"/> marks the
        /// slot the rebuilt record is written back into.</summary>
        public List<EsfNode> TailFields { get; private set; } = [];
        public int TailSceneRecordIndex { get; private set; } = -1;

        public float Duration
        {
            get => HeaderFields.Count > 0 && HeaderFields[0].Value is float f ? f : 20f;
            set
            {
                if (HeaderFields.Count > 0 && HeaderFields[0].Kind == EsfNodeKind.F32)
                    HeaderFields[0] = EsfNode.Leaf(EsfNodeKind.F32, value, HeaderFields[0].Optimized);
            }
        }

        /// <summary>ROOT header field 1 (Coord3d, present in every version): a world-space focus/
        /// anchor point for the scene - corpus-confirmed usually zero, non-zero only
        /// on dramatic set-piece scenes, and never matching a placed element's own position, so
        /// it's independently authored rather than derived.</summary>
        public Vector3 FocusPoint
        {
            get => HeaderFields.Count > 1 && HeaderFields[1].Value is Coord3d c ? new Vector3(c.X, c.Y, c.Z) : Vector3.Zero;
            set
            {
                if (HeaderFields.Count > 1 && HeaderFields[1].Kind == EsfNodeKind.Coord3d)
                    HeaderFields[1] = EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(value.X, value.Y, value.Z), HeaderFields[1].Optimized);
            }
        }

        /// <summary>ROOT header field 2 (F32, present in every version): a secondary, rarely-used
        /// distance/radius - corpus-confirmed dominated by sentinel values 0 ("unset")
        /// and -1 ("disabled"), otherwise a real round distance, usually smaller than Duration.</summary>
        public float Radius
        {
            get => HeaderFields.Count > 2 && HeaderFields[2].Value is float f ? f : 0f;
            set
            {
                if (HeaderFields.Count > 2 && HeaderFields[2].Kind == EsfNodeKind.F32)
                    HeaderFields[2] = EsfNode.Leaf(EsfNodeKind.F32, value, HeaderFields[2].Optimized);
            }
        }

        /// <summary>Whether this file's ROOT version has the weather/environment path header field
        /// at all (v1 doesn't - its header is only the leading Duration/FocusPoint/Radius trio).</summary>
        public bool HasWeatherPath => HeaderFields.Count > 3;

        /// <summary>ROOT header field 3 (Ascii, v2+ only): a weather/environment preset asset path
        /// - corpus-confirmed as a real .environment file path on the ~5% of files
        /// that use it (frontend lord-select-screen ambience scenes and similar), empty otherwise.</summary>
        public string WeatherPath
        {
            get => HeaderFields.Count > 3 && HeaderFields[3].Value is string s ? s : "";
            set
            {
                if (HeaderFields.Count > 3 && HeaderFields[3].Kind == EsfNodeKind.Ascii)
                    HeaderFields[3] = EsfNode.Leaf(EsfNodeKind.Ascii, value, HeaderFields[3].Optimized);
            }
        }

        /// <summary>Whether nested elements sit after their carrier's attach-tree children (rather
        /// than directly after the carrier) in the manifest's DFS order. Resolved per file at load
        /// by validating manifest keyframe counts against the actual channel data.</summary>
        public bool NestedAfterAttachChildren { get; private set; }

        public IEnumerable<CscElement> AllElementsIncludingNested()
        {
            foreach (var element in Elements)
            {
                yield return element;
                foreach (var nested in NestedDescendants(element))
                    yield return nested;
            }
        }

        static IEnumerable<CscElement> NestedDescendants(CscElement element)
        {
            foreach (var child in element.Children.Where(c => c.IsNested))
            {
                yield return child;
                foreach (var deeper in NestedDescendants(child))
                    yield return deeper;
            }
        }

        public CscElement? FindElement(int id) => AllElementsIncludingNested().FirstOrDefault(e => e.Id == id);

        public int NextFreeElementId()
        {
            var all = AllElementsIncludingNested().ToList();
            return all.Count == 0 ? 0 : all.Max(e => e.Id) + 1;
        }

        /// <summary>Attach-tree depth-first order: roots in attach order, then children recursively
        /// - the order COMPOSITE_SCENE's manifest groups are stored in (corpus-verified). Nested
        /// elements (inside variant models) are included only when <paramref name="includeNested"/>
        /// is set, positioned according to <see cref="NestedAfterAttachChildren"/>.</summary>
        public List<CscElement> DfsOrder(bool includeNested = false)
        {
            var result = new List<CscElement>();
            void Visit(CscElement e)
            {
                result.Add(e);
                var nested = e.Children.Where(c => c.IsNested);
                var attached = e.Children.Where(c => !c.IsNested);
                var ordered = NestedAfterAttachChildren ? attached.Concat(nested) : nested.Concat(attached);
                foreach (var child in ordered)
                {
                    if (child.IsNested && !includeNested)
                        continue;
                    Visit(child);
                }
            }
            foreach (var root in RootElements)
                Visit(root);
            return result;
        }

        // ---------------------------------------------------------------------
        // Loading
        // ---------------------------------------------------------------------

        public static CscScene Load(byte[] fileData)
        {
            var scene = new CscScene();
            using var stream = new MemoryStream(fileData);
            scene.Document = EsfReader.Read(stream);

            var root = scene.Document.Root;
            if (root.Groups == null || root.Groups.Count != 1)
                throw new InvalidDataException("ROOT record does not have the expected single field group.");

            var rootFields = root.Groups[0];
            scene.RootVersion = root.Version;

            var structure = RootStructureDetector.Detect(rootFields, root.Version)
                ?? throw new InvalidDataException(
                    $"ROOT v{root.Version} did not match the known .csc scene grammar - the file may use an unseen layout.");

            scene.HeaderFields = rootFields.Take(structure.HeaderLength).ToList();

            foreach (var group in structure.ElementGroups)
            {
                var element = DecodeElement(group);
                element.Scene = scene;
                scene.Elements.Add(element);
            }

            // Attach tree: every element appears exactly once as a child (of -1 or a real parent).
            var elementById = scene.Elements.ToDictionary(e => e.Id);
            foreach (var attachGroup in structure.AttachGroups)
            {
                var parent = attachGroup.ParentId >= 0 ? elementById.GetValueOrDefault(attachGroup.ParentId) : null;
                foreach (var childInfo in attachGroup.Children)
                {
                    if (!elementById.TryGetValue(childInfo.ElementId, out var child))
                        continue;

                    child.AttachBoneIndex = childInfo.AttachValue;
                    if (rootFields[childInfo.StartIndex + 1] is { IsRecord: true } attachRecord)
                        child.AttachRecord = attachRecord;

                    if (parent != null)
                    {
                        child.Parent = parent;
                        parent.Children.Add(child);
                    }
                    else
                    {
                        scene.RootElements.Add(child);
                    }
                }
            }

            scene.LoadTail(rootFields, structure.TailStartIndex);
            return scene;
        }

        /// <summary>Checks the attach tree before it is written: no duplicate ids, every element
        /// reachable exactly once from <see cref="RootElements"/>, parent links consistent and no
        /// cycles. Throws <see cref="InvalidDataException"/> otherwise.</summary>
        internal void ValidateForWrite()
        {
            var ids = new HashSet<int>();
            foreach (var element in AllElementsIncludingNested())
            {
                if (!ids.Add(element.Id))
                    throw new InvalidDataException($"The scene contains duplicate element id {element.Id}.");
            }

            var mainElements = Elements.ToHashSet();
            var seen = new HashSet<CscElement>();
            foreach (var root in RootElements)
            {
                if (!mainElements.Contains(root) || root.IsNested || root.Parent != null || !seen.Add(root))
                    throw new InvalidDataException($"Element {root.Id} has an invalid root attachment.");
            }

            foreach (var parent in Elements)
            {
                foreach (var child in parent.Children.Where(c => !c.IsNested))
                {
                    if (!mainElements.Contains(child))
                        throw new InvalidDataException($"Attach child {child.Id} is not part of the scene.");
                    if (!ReferenceEquals(child.Parent, parent))
                        throw new InvalidDataException($"Attach child {child.Id} has an inconsistent parent.");
                    if (!seen.Add(child))
                        throw new InvalidDataException($"Attach child {child.Id} appears more than once.");
                }
            }

            foreach (var element in Elements)
            {
                if (!seen.Contains(element))
                    throw new InvalidDataException($"Element {element.Id} is missing from the attach hierarchy.");

                var chain = new HashSet<CscElement>();
                for (var current = element; current != null; current = current.Parent)
                {
                    if (!chain.Add(current))
                        throw new InvalidDataException($"Attach hierarchy contains a cycle at element {current.Id}.");
                }
            }
        }

        void LoadTail(List<EsfNode> rootFields, int tailStartIndex)
        {
            TailFields = rootFields.Skip(tailStartIndex).ToList();
            TailSceneRecordIndex = TailFields.FindIndex(f => f is { IsRecord: true, Name: "COMPOSITE_SCENE" });

            List<EsfNode> manifestRegion;
            if (TailSceneRecordIndex >= 0)
            {
                var sceneRecord = TailFields[TailSceneRecordIndex];
                ManifestKind = CscManifestKind.WrappedRecord;
                CompositeSceneRecordName = sceneRecord.Name!;
                CompositeSceneVersion = sceneRecord.Version;
                CompositeSceneFlags = sceneRecord.RecordFlags;
                if (sceneRecord.Groups == null || sceneRecord.Groups.Count != 1)
                    return;
                manifestRegion = sceneRecord.Groups[0];
            }
            else if (TailFields.Count > 0)
            {
                ManifestKind = CscManifestKind.Inline;
                manifestRegion = TailFields;
            }
            else
            {
                ManifestKind = CscManifestKind.None;
                ManifestParsed = true;
                return;
            }

            ParseManifest(manifestRegion);
        }

        void ParseManifest(List<EsfNode> region)
        {
            // Leading I32(groupCount) + groups in attach-tree DFS order.
            if (region.Count == 0 || region[0].Kind != EsfNodeKind.I32 || region[0].Value is not int groupCount || groupCount < 0)
                return;

            var groups = CompositeSceneDetector.DetectGroups(region);

            // DetectGroups scans the whole region; only the run starting right after the count
            // field, containing exactly groupCount groups, is the real manifest.
            groups = groups.Where(g => g.StartIndex >= 1).Take(groupCount).ToList();
            if (groups.Count != groupCount)
                return;

            // Contiguity check: groups must butt up against each other starting at field 1.
            var expectedStart = 1;
            foreach (var group in groups)
            {
                if (group.StartIndex != expectedStart)
                    return;
                expectedStart += group.FieldCount;
            }

            // Resolve the DFS order over all elements (nested included). The manifest is a
            // keyframe-count restatement, so a candidate order is provably right when every
            // group's counts match its element's actual channel keyframe counts - this also
            // settles where nested elements sit relative to attach children per file.
            List<CscElement>? dfs = null;
            foreach (var nestedAfter in new[] { false, true })
            {
                NestedAfterAttachChildren = nestedAfter;
                var candidate = DfsOrder(includeNested: true);
                if (candidate.Count == groupCount && ManifestCountsMatch(candidate, groups))
                {
                    dfs = candidate;
                    break;
                }
            }

            if (dfs == null)
            {
                NestedAfterAttachChildren = false;
                return; // unverifiable mapping (e.g. legacy layouts) - keep the manifest verbatim.
            }

            for (var i = 0; i < dfs.Count; i++)
            {
                var g = groups[i];
                dfs[i].ManifestGroupFields = region.Skip(g.StartIndex).Take(g.FieldCount).ToList();
            }

            var pos = expectedStart;
            if (CompositeSceneDetector.DetectEntrySection(region, pos) is not { } entrySection)
                return;
            pos += entrySection.FieldCount;

            foreach (var entry in entrySection.Entries)
            {
                ManifestEntries.Add(new CscManifestEntry
                {
                    Fields = region.Skip(entry.StartIndex).Take(entry.FieldCount).ToList(),
                    ElementIds = entry.ElementIds.ToList(),
                });
            }

            if (CompositeSceneDetector.DetectFooter(region, pos, entrySection.Entries.Count) is not { } footer)
            {
                ManifestEntries.Clear();
                return;
            }
            pos += footer.FieldCount;

            ManifestLeftoverFields = region.Skip(pos).ToList();
            ManifestParsed = true;
        }

        static bool ManifestCountsMatch(List<CscElement> candidate, List<CompositeSceneDetector.GroupInfo> groups)
        {
            for (var i = 0; i < candidate.Count; i++)
            {
                var manifestCounts = groups[i].Channels
                    .SelectMany(c => c.SubComponents)
                    .Select(s => s.KeyframeCount);
                if (!manifestCounts.SequenceEqual(candidate[i].AllChannelKeyframeCounts()))
                    return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------
        // Element decoding
        // ---------------------------------------------------------------------

        static readonly Dictionary<string, CscElementKind> _kindByRecordName = new()
        {
            ["MODEL_ELEMENT"] = CscElementKind.Model,
            ["VARIANT_MODEL_ELEMENT"] = CscElementKind.VariantModel,
            ["VFX_ELEMENT"] = CscElementKind.Vfx,
            ["SFX_ELEMENT"] = CscElementKind.Sfx,
            ["ANIMATION_ELEMENT"] = CscElementKind.Animation,
            ["ANIMATION_NODE_TRANSFORM_ELEMENT"] = CscElementKind.AnimationNodeTransform,
            ["CAMERA_ELEMENT"] = CscElementKind.Camera,
            ["POINT_LIGHT_ELEMENT"] = CscElementKind.PointLight,
            ["SPOT_LIGHT_ELEMENT"] = CscElementKind.SpotLight,
            ["PREFAB_ELEMENT"] = CscElementKind.Prefab,
            ["ROOT_REF_ELEMENT"] = CscElementKind.RootRef,
            ["SOUND_SPHERE_ELEMENT"] = CscElementKind.SoundSphere,
            ["CAMERA_SHAKE_ELEMENT"] = CscElementKind.CameraShake,
        };

        static CscElement DecodeElement(RootStructureDetector.ElementGroupInfo group) =>
            DecodeElementBundle(group.Label, group.Records.ToList());

        static CscElement DecodeElementBundle(string label, List<EsfNode> records)
        {
            var element = new CscElement
            {
                GroupLabel = label,
                Records = records,
            };

            element.ElementRecord = records.FirstOrDefault(r => r.Name == "ELEMENT");
            element.PeriodRecord = records.FirstOrDefault(r => r.Name == "ELEMENT_PERIOD");
            element.SpliceRecord = records.FirstOrDefault(r => r.Name == "ANIMATION_SPLICE_ELEMENT");

            foreach (var record in records)
            {
                if (record.Name != null && _kindByRecordName.TryGetValue(record.Name, out var kind))
                {
                    element.Kind = kind;
                    element.TypeRecord = record;
                    break;
                }
            }
            if (element.TypeRecord == null)
                element.Kind = CscElementKind.Locator;

            if (element.ElementRecord != null)
                DecodeElementRecord(element, element.ElementRecord);
            if (element.PeriodRecord != null)
                DecodePeriodRecord(element, element.PeriodRecord);
            if (element.SpliceRecord != null)
                DecodeSpliceRecord(element, element.SpliceRecord);
            if (element.TypeRecord != null)
                DecodeTypeRecord(element, element.TypeRecord);

            return element;
        }

        static void DecodeElementRecord(CscElement element, EsfNode record)
        {
            element.ElementVersion = record.Version;
            var fields = record.Children.ToList();

            if (fields.Count > 0 && fields[0].Value is uint id)
                element.Id = (int)id;
            if (fields.Count > 1 && fields[1].Value is string headerString)
                element.HeaderString = headerString;
            if (fields.Count > 3 && fields[2].Value is float begin && fields[3].Value is float end)
            {
                element.Begin = begin;
                element.End = end;
            }
            if (fields.Count > 5 && fields[4].Value is string timing && fields[5].Value is string anchor)
            {
                element.TimingMode = timing;
                element.AnchorMode = anchor;
            }

            // The v6/v7/v100 shape: header(6) + Group(3) pos + Group(3) rot + Group(1) scale
            // + Group(1) weight + Coord3d + Coord3d + trailing extras. Anything else (legacy v5)
            // is preserved verbatim and read-only.
            var groups = CurveDetector.DetectGroups(fields);
            var contiguous = groups.Count == 4 && groups[0].StartIndex == 6;
            if (contiguous)
            {
                var expected = 6;
                foreach (var g in groups)
                {
                    if (g.StartIndex != expected)
                    {
                        contiguous = false;
                        break;
                    }
                    expected += g.FieldCount;
                }

                var trailingStart = expected;
                contiguous = contiguous
                    && groups[0].Channels.Count == 3 && groups[1].Channels.Count == 3
                    && groups[2].Channels.Count == 1 && groups[3].Channels.Count == 1
                    && trailingStart + 1 < fields.Count
                    && fields[trailingStart].Kind == EsfNodeKind.Coord3d
                    && fields[trailingStart + 1].Kind == EsfNodeKind.Coord3d;

                if (contiguous)
                {
                    element.Position = ToChannelGroup(fields, groups[0]);
                    element.Rotation = ToChannelGroup(fields, groups[1]);
                    element.Scale = ToChannelGroup(fields, groups[2]);
                    element.Weight = ToChannelGroup(fields, groups[3]);

                    var basePos = (Coord3d)fields[trailingStart].Value!;
                    var baseRot = (Coord3d)fields[trailingStart + 1].Value!;
                    element.BasePosition = new Vector3(basePos.X, basePos.Y, basePos.Z);
                    element.BaseRotation = new Vector3(baseRot.X, baseRot.Y, baseRot.Z);
                    element.ElementTrailingFields = fields.Skip(trailingStart + 2).ToList();
                }
            }

            if (!contiguous)
                element.IsLegacyRaw = true;
        }

        static void DecodePeriodRecord(CscElement element, EsfNode record)
        {
            var fields = record.Children.ToList();
            if (fields.Count >= 3 && fields[0].Value is bool flag && fields[1].Value is float speed && fields[2].Value is float offset)
            {
                element.PeriodFlag = flag;
                element.PeriodSpeedMultiplier = speed;
                element.PeriodTimeOffset = offset;
            }
        }

        static void DecodeSpliceRecord(CscElement element, EsfNode record)
        {
            var fields = record.Children.ToList();
            if (fields.Count > 0 && fields[0].Value is uint bone)
                element.SpliceBoneId = unchecked((int)bone);
            if (fields.Count > 1 && fields[1].Value is uint depthA)
                element.SpliceDepthA = unchecked((int)depthA);
            if (fields.Count > 2 && fields[2].Value is uint depthB)
                element.SpliceDepthB = unchecked((int)depthB);
            element.SpliceRemainingFieldsDisplay = string.Join(", ", fields.Skip(3).Select(f => $"{f.Kind}={f.Value}"));
        }

        static void DecodeTypeRecord(CscElement element, EsfNode record)
        {
            var fields = record.Children.ToList();

            if (element.Kind == CscElementKind.AnimationNodeTransform)
            {
                if (fields.Count > 0 && fields[0].Value is uint boneId)
                    element.NodeTransformBoneId = unchecked((int)boneId);
                if (fields.Count > 1 && fields[1].Value is uint second)
                    element.NodeTransformSecondValue = unchecked((int)second);
            }

            if (fields.Count > 0 && fields[0].Kind == EsfNodeKind.Ascii && fields[0].Value is string path)
                element.AssetPath = path;
            if (element.Kind == CscElementKind.Sfx)
            {
                if (fields.Count > 1 && fields[1].Value is string stopEvent)
                    element.SfxStopEvent = stopEvent;

                // Versions with 4+ leading Ascii fields carry a second, independent (start, stop)
                // Wwise event pair - confirmed, see CscElement.SfxHasSecondEventPair.
                if (fields.Count > 3 && fields[2].Kind == EsfNodeKind.Ascii && fields[3].Kind == EsfNodeKind.Ascii)
                {
                    element.SfxHasSecondEventPair = true;
                    element.SfxEvent2Start = fields[2].Value as string ?? "";
                    element.SfxEvent2Stop = fields[3].Value as string ?? "";
                }
            }

            // Camera/light/root-ref records carry the same Group/Channel/Curve grammar as ELEMENT.
            if (element.Kind is CscElementKind.Camera or CscElementKind.PointLight or CscElementKind.SpotLight or CscElementKind.RootRef)
            {
                var runs = CurveDetector.DetectGroups(fields);
                foreach (var run in runs)
                    element.TypeGroups.Add(ToChannelGroup(fields, run));

                // Every CAMERA_ELEMENT (any version) carries an extra Bool between the roll and near
                // groups (group indexes 1 and 2) that isn't part of the Group/Channel/Curve grammar
                // itself - corpus-confirmed field order (fov, roll, bool, near, far), meaning not yet identified.
                if (element.Kind == CscElementKind.Camera && runs.Count > 1)
                {
                    var boolIndex = runs[1].StartIndex + runs[1].FieldCount;
                    if (boolIndex < fields.Count && fields[boolIndex].Kind == EsfNodeKind.Bool)
                    {
                        element.CameraUnknownFlagNode = fields[boolIndex];
                        element.CameraUnknownFlag = fields[boolIndex].Value is true;
                    }
                }
            }

            DecodeNestedElements(element, fields);
        }

        /// <summary>
        /// Type records (variant models especially) can carry nested ELEMENT bundles as record
        /// nodes among their fields - e.g. a prop's idle animation attached to the prop itself.
        /// Each bundle is an ELEMENT record plus the immediately following non-ELEMENT records.
        /// </summary>
        static void DecodeNestedElements(CscElement carrier, List<EsfNode> fields)
        {
            var i = 0;
            while (i < fields.Count)
            {
                if (fields[i] is not { IsRecord: true, Name: "ELEMENT" })
                {
                    i++;
                    continue;
                }

                var bundle = new List<EsfNode> { fields[i] };
                var j = i + 1;
                while (j < fields.Count && fields[j].IsRecord && fields[j].Name != "ELEMENT")
                    bundle.Add(fields[j++]);

                var nested = DecodeElementBundle("nested", bundle);
                nested.IsNested = true;
                nested.Parent = carrier;
                carrier.Children.Add(nested);
                i = j;
            }
        }

        internal static CscChannelGroup ToChannelGroup(List<EsfNode> fields, CurveDetector.GroupRun run)
        {
            var group = new CscChannelGroup
            {
                Marker = fields[run.StartIndex].Value is uint marker ? marker : 1,
            };

            foreach (var channelRun in run.Channels)
            {
                var channel = new CscChannel
                {
                    Header = channelRun.Curve.Header,
                    HasModeTags = channelRun.HeaderFieldCount == 3,
                };
                if (channel.HasModeTags)
                {
                    channel.ModeA = (string)fields[channelRun.StartIndex + 1].Value!;
                    channel.ModeB = (string)fields[channelRun.StartIndex + 2].Value!;
                }

                foreach (var point in channelRun.Curve.Points)
                {
                    channel.Keyframes.Add(new CscKeyframe
                    {
                        Time = point.X,
                        Value = point.Y,
                        ModeIn = point.ModeIn,
                        TangentIn = point.TangentIn,
                        TangentOut = point.TangentOut,
                        ModeOut = point.ModeOut,
                    });
                }

                group.Channels.Add(channel);
            }

            return group;
        }

        // ---------------------------------------------------------------------
        // Structural edits
        // ---------------------------------------------------------------------

        /// <summary>Structure edits restate the COMPOSITE_SCENE manifest on save. When that
        /// manifest could not be parsed it is written back verbatim, so an added, removed or
        /// moved element would leave it describing the wrong tree.</summary>
        void EnsureStructureEditable()
        {
            if (!ManifestParsed)
                throw new InvalidOperationException(
                    "Scene structure cannot be edited because the COMPOSITE_SCENE manifest could not be parsed.");
        }

        public void AddElement(CscElement element, CscElement? parent = null)
        {
            EnsureStructureEditable();
            element.Scene = this;
            Elements.Add(element);
            if (parent != null)
            {
                element.Parent = parent;
                parent.Children.Add(element);
            }
            else
            {
                RootElements.Add(element);
            }
        }

        public void RemoveElementSubtree(CscElement element)
        {
            if (element.IsNested || element.IsExternal)
                return; // nested/external elements are not part of this scene's editable data

            EnsureStructureEditable();
            foreach (var child in element.Children.Where(c => !c.IsNested).ToList())
                RemoveElementSubtree(child);

            // Nested descendants disappear with the carrier - drop their manifest entries too.
            foreach (var nested in NestedDescendants(element))
                ManifestEntries.RemoveAll(entry => entry.ElementIds.Contains(nested.Id));

            if (element.Parent != null)
                element.Parent.Children.Remove(element);
            else
                RootElements.Remove(element);

            Elements.Remove(element);

            var deadId = element.Id;
            ManifestEntries.RemoveAll(entry => entry.ElementIds.Contains(deadId));
        }

        /// <summary>Reparents an element (null = make top-level). Returns false when the move
        /// would create a cycle.</summary>
        public bool Reparent(CscElement element, CscElement? newParent)
        {
            if (element.IsNested || (newParent?.IsNested ?? false))
                return false; // nested elements are bound to their carrier record
            if (element.IsExternal || (newParent?.IsExternal ?? false))
                return false; // root-ref sub-scene elements are display-only

            EnsureStructureEditable();
            if (newParent == element || (newParent != null && newParent.IsInSubtreeOf(element)))
                return false;

            if (element.Parent != null)
                element.Parent.Children.Remove(element);
            else
                RootElements.Remove(element);

            element.Parent = newParent;
            if (newParent != null)
                newParent.Children.Add(element);
            else
                RootElements.Add(element);

            return true;
        }
    }
}
