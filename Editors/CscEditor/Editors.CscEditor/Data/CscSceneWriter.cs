using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Csc;
using Shared.GameFormats.Esf;

namespace Editors.CscEditor.Data
{
    /// <summary>
    /// Serializes an edited <see cref="CscScene"/> back to .csc bytes. Rebuilds exactly the parts
    /// the editor models - ELEMENT records, the attach tree, and COMPOSITE_SCENE's manifest/
    /// entries/footer - and passes every other field through verbatim from the loaded file.
    /// The output is re-read and re-detected as a self-check before being returned.
    /// </summary>
    public static class CscSceneWriter
    {
        public static byte[] Write(CscScene scene)
        {
            ArgumentNullException.ThrowIfNull(scene);
            scene.ValidateForWrite();

            var rootFields = new List<EsfNode>(scene.HeaderFields);

            // Element groups, in scene order.
            rootFields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)scene.Elements.Count, optimized: true));
            foreach (var element in scene.Elements)
            {
                rootFields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, element.GroupLabel));
                rootFields.AddRange(RebuildElementRecords(element));
            }

            // Attach tree: the -1 bucket (top-level elements) first, then a bucket per parent that
            // has children, in DFS order - every top-level element appears exactly once as a
            // child. Nested elements live inside their carrier's type record, not here.
            var buckets = new List<(int ParentId, List<CscElement> Children)>();
            if (scene.RootElements.Count > 0)
                buckets.Add((-1, scene.RootElements.ToList()));
            foreach (var element in scene.DfsOrder())
            {
                var attachChildren = element.Children.Where(c => !c.IsNested).ToList();
                if (attachChildren.Count > 0)
                    buckets.Add((element.Id, attachChildren));
            }

            rootFields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)buckets.Count, optimized: true));
            foreach (var (parentId, children) in buckets)
            {
                rootFields.Add(EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)parentId), optimized: true));
                rootFields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)children.Count, optimized: true));
                foreach (var child in children)
                {
                    rootFields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)child.Id, optimized: true));
                    rootFields.Add(BuildAttachRecord(child));
                }
            }

            rootFields.AddRange(RebuildTail(scene));

            var oldRoot = scene.Document.Root;
            var newRoot = EsfNode.NewRecord(oldRoot.Name!, oldRoot.Version, oldRoot.RecordFlags, [rootFields]);
            var newDocument = new EsfDocument
            {
                Signature = scene.Document.Signature,
                Unknown1 = scene.Document.Unknown1,
                CreationDate = scene.Document.CreationDate,
                Root = newRoot,
            };

            using var stream = new MemoryStream();
            EsfWriter.Write(newDocument, stream);
            var bytes = stream.ToArray();

            SelfCheck(scene, bytes);
            return bytes;
        }

        static void SelfCheck(CscScene scene, byte[] bytes)
        {
            CscScene reread;
            try
            {
                reread = CscScene.Load(bytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Save self-check failed: the written file does not reload as a valid scene. The file was NOT saved.", ex);
            }

            var expectedById = scene.Elements.ToDictionary(e => e.Id);
            var actualById = reread.Elements.ToDictionary(e => e.Id);
            if (!expectedById.Keys.Order().SequenceEqual(actualById.Keys.Order()))
                throw new InvalidOperationException(
                    "Save self-check failed: the element id set changed during serialization. The file was NOT saved.");

            foreach (var (id, expected) in expectedById)
            {
                var actual = actualById[id];
                if (actual.Parent?.Id != expected.Parent?.Id ||
                    actual.AttachBoneIndex != expected.AttachBoneIndex ||
                    actual.Kind != expected.Kind ||
                    actual.AssetPath != expected.AssetPath)
                {
                    throw new InvalidOperationException(
                        $"Save self-check failed: element {id} changed during serialization. The file was NOT saved.");
                }
            }
        }

        // ---------------------------------------------------------------------
        // Element records
        // ---------------------------------------------------------------------

        static List<EsfNode> RebuildElementRecords(CscElement element)
        {
            var records = new List<EsfNode>();
            foreach (var record in element.Records)
            {
                if (element.IsLegacyRaw)
                {
                    records.Add(record);
                    continue;
                }

                if (record == element.ElementRecord)
                    records.Add(RebuildElementRecord(element, record));
                else if (record == element.PeriodRecord)
                    records.Add(RebuildPeriodRecord(element, record));
                else if (record == element.SpliceRecord)
                    records.Add(RebuildSpliceRecord(element, record));
                else if (record == element.TypeRecord)
                    records.Add(RebuildTypeRecord(element, record));
                else
                    records.Add(record);
            }
            return records;
        }

        static EsfNode RebuildRecord(EsfNode original, List<EsfNode> newFields) =>
            EsfNode.NewRecord(original.Name!, original.Version, original.RecordFlags, [newFields]);

        static bool HasSingleGroup(EsfNode record) => record.Groups is { Count: 1 };

        static EsfNode RebuildElementRecord(CscElement element, EsfNode record)
        {
            if (element.IsStaticTransform)
                return RebuildStaticElementRecord(element, record);

            if (!HasSingleGroup(record) || element.Position == null || element.Rotation == null ||
                element.Scale == null || element.Weight == null)
                return record;

            var fields = new List<EsfNode>
            {
                EsfNode.Leaf(EsfNodeKind.U32, (uint)element.Id, optimized: true),
                EsfNode.Leaf(EsfNodeKind.Ascii, element.HeaderString),
                EsfNode.Leaf(EsfNodeKind.F32, element.Begin, optimized: element.Begin == 0),
                EsfNode.Leaf(EsfNodeKind.F32, element.End),
                EsfNode.Leaf(EsfNodeKind.Ascii, element.TimingMode),
                EsfNode.Leaf(EsfNodeKind.Ascii, element.AnchorMode),
            };

            foreach (var group in element.ElementGroups())
                AppendChannelGroup(fields, group);

            fields.Add(EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(element.BasePosition.X, element.BasePosition.Y, element.BasePosition.Z)));
            fields.Add(EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(element.BaseRotation.X, element.BaseRotation.Y, element.BaseRotation.Z)));
            fields.AddRange(element.ElementTrailingFields);

            return RebuildRecord(record, fields);
        }

        static EsfNode RebuildPeriodRecord(CscElement element, EsfNode record)
        {
            if (!HasSingleGroup(record) || record.Groups![0].Count < 3)
                return record;

            var fields = record.Groups[0].ToList();
            fields[0] = EsfNode.Leaf(EsfNodeKind.Bool, element.PeriodFlag, fields[0].Optimized);
            fields[1] = EsfNode.Leaf(EsfNodeKind.F32, element.PeriodSpeedMultiplier, fields[1].Optimized && element.PeriodSpeedMultiplier == 0);
            fields[2] = EsfNode.Leaf(EsfNodeKind.F32, element.PeriodTimeOffset, fields[2].Optimized && element.PeriodTimeOffset == 0);
            return RebuildRecord(record, fields);
        }

        static EsfNode RebuildSpliceRecord(CscElement element, EsfNode record)
        {
            if (!HasSingleGroup(record) || record.Groups![0].Count < 3)
                return record;

            var fields = record.Groups[0].ToList();
            fields[0] = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.SpliceBoneId), fields[0].Optimized);
            fields[1] = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.SpliceDepthA), fields[1].Optimized);
            fields[2] = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.SpliceDepthB), fields[2].Optimized);
            return RebuildRecord(record, fields);
        }

        /// <summary>ELEMENT v5: the fixed 17-field transform (see CscElement.IsStaticTransform).</summary>
        static EsfNode RebuildStaticElementRecord(CscElement element, EsfNode record)
        {
            if (new[] { element.Position, element.Rotation, element.Scale }.Any(g => g?.Channels.Any(c => c.Keyframes.Count != 0) == true))
                throw new InvalidDataException($"Element {element.Id}: ELEMENT v5 stores a fixed transform and can't hold keyframes.");

            var fields = record.Children.ToList();
            var position = element.EvaluatePosition(0);
            var rotation = element.CurrentStaticRotation();
            var baseRotation = element.CurrentStaticBaseRotation();
            float[] rotationComponents = [rotation.X, rotation.Y, rotation.Z, rotation.W];
            float[] baseRotationComponents = [baseRotation.X, baseRotation.Y, baseRotation.Z, baseRotation.W];

            fields[0] = Keep(fields[0], (uint)element.Id, () => EsfNode.Leaf(EsfNodeKind.U32, (uint)element.Id, fields[0].Optimized));
            fields[1] = Keep(fields[1], element.HeaderString, () => EsfNode.Leaf(EsfNodeKind.Ascii, element.HeaderString));
            fields[2] = StaticFloat(fields[2], element.Begin);
            fields[3] = StaticFloat(fields[3], element.End);
            fields[4] = Keep(fields[4], element.TimingMode, () => EsfNode.Leaf(EsfNodeKind.Ascii, element.TimingMode));
            fields[5] = Keep(fields[5], element.AnchorMode, () => EsfNode.Leaf(EsfNodeKind.Ascii, element.AnchorMode));
            fields[6] = StaticCoord(fields[6], position);
            for (var i = 0; i < 4; i++)
                fields[7 + i] = StaticFloat(fields[7 + i], rotationComponents[i]);
            fields[11] = StaticFloat(fields[11], element.EvaluateScale(0));
            fields[12] = StaticCoord(fields[12], element.BasePosition);
            for (var i = 0; i < 4; i++)
                fields[13 + i] = StaticFloat(fields[13 + i], baseRotationComponents[i]);
            return RebuildRecord(record, fields);
        }

        /// <summary>Keeps the original node (and so its optimized/compact encoding) when the value
        /// didn't change, so an untouched v5 record writes back byte-identical.</summary>
        static EsfNode StaticFloat(EsfNode original, float value) =>
            original.Value is float old && old.Equals(value) ? original : EsfNode.Leaf(EsfNodeKind.F32, value);

        static EsfNode StaticCoord(EsfNode original, Vector3 value) =>
            original.Value is Coord3d old && old.X.Equals(value.X) && old.Y.Equals(value.Y) && old.Z.Equals(value.Z)
                ? original
                : EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(value.X, value.Y, value.Z));

        static EsfNode Keep(EsfNode original, object value, Func<EsfNode> rebuild) =>
            Equals(original.Value, value) ? original : rebuild();

        static EsfNode RebuildTypeRecord(CscElement element, EsfNode record)
        {
            if (!HasSingleGroup(record))
                return record;

            var originalFields = record.Groups![0];
            if (element.StaticCameraFov != null)
            {
                if (new[] { element.StaticCameraFov, element.StaticCameraNear, element.StaticCameraFar }.Any(c => c?.Keyframes.Count > 0))
                    throw new InvalidDataException($"Element {element.Id}: CAMERA_ELEMENT v{record.Version} stores fixed values and can't hold keyframes.");

                var staticFields = originalFields.ToList();
                staticFields[0] = StaticFloat(staticFields[0], element.StaticCameraFov.StaticValue);
                if (element.StaticCameraNear != null && element.StaticCameraFar != null)
                {
                    staticFields[1] = StaticFloat(staticFields[1], element.StaticCameraNear.StaticValue);
                    staticFields[2] = StaticFloat(staticFields[2], element.StaticCameraFar.StaticValue);
                    if (staticFields[3].Value is not bool flag || flag != element.CameraUnknownFlag)
                        staticFields[3] = EsfNode.Leaf(EsfNodeKind.Bool, element.CameraUnknownFlag);
                }
                return RebuildRecord(record, staticFields);
            }
            var fields = new List<EsfNode>(originalFields.Count);

            // Channel-group runs are re-detected on the original fields and spliced with the
            // edited groups; everything between/around them passes through verbatim.
            var runs = element.Kind is CscElementKind.Camera or CscElementKind.PointLight or CscElementKind.SpotLight or CscElementKind.RootRef
                ? CurveDetector.DetectGroups(originalFields)
                : [];
            var canSpliceGroups = runs.Count == element.TypeGroups.Count;

            // Nested elements (inside variant models etc.) have their records rebuilt in place.
            var nestedRecordReplacements = new Dictionary<EsfNode, EsfNode>();
            foreach (var nested in element.Children.Where(c => c.IsNested))
            {
                var rebuilt = RebuildElementRecords(nested);
                for (var r = 0; r < nested.Records.Count; r++)
                    nestedRecordReplacements[nested.Records[r]] = rebuilt[r];
            }

            var i = 0;
            var runIndex = 0;
            while (i < originalFields.Count)
            {
                if (canSpliceGroups && runIndex < runs.Count && runs[runIndex].StartIndex == i)
                {
                    AppendChannelGroup(fields, element.TypeGroups[runIndex]);
                    i += runs[runIndex].FieldCount;
                    runIndex++;
                    continue;
                }

                if (nestedRecordReplacements.TryGetValue(originalFields[i], out var replacement))
                {
                    fields.Add(replacement);
                    i++;
                    continue;
                }

                if (element.CameraUnknownFlagNode != null && ReferenceEquals(originalFields[i], element.CameraUnknownFlagNode))
                {
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Bool, element.CameraUnknownFlag));
                    i++;
                    continue;
                }

                fields.Add(originalFields[i]);
                i++;
            }

            if (element.Kind == CscElementKind.AnimationNodeTransform)
            {
                if (fields.Count > 0 && fields[0].Kind == EsfNodeKind.U32)
                    fields[0] = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.NodeTransformBoneId), fields[0].Optimized);
                if (fields.Count > 1 && fields[1].Kind == EsfNodeKind.U32)
                    fields[1] = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.NodeTransformSecondValue), fields[1].Optimized);
            }

            // Asset path / event-name patches on the leading Ascii fields.
            if (fields.Count > 0 && fields[0].Kind == EsfNodeKind.Ascii)
                fields[0] = EsfNode.Leaf(EsfNodeKind.Ascii, element.AssetPath);
            if (element.Kind == CscElementKind.Sfx)
            {
                if (fields.Count > 1 && fields[1].Kind == EsfNodeKind.Ascii)
                    fields[1] = EsfNode.Leaf(EsfNodeKind.Ascii, element.SfxStopEvent);
                if (element.SfxHasSecondEventPair && fields.Count > 3 &&
                    fields[2].Kind == EsfNodeKind.Ascii && fields[3].Kind == EsfNodeKind.Ascii)
                {
                    fields[2] = EsfNode.Leaf(EsfNodeKind.Ascii, element.SfxEvent2Start);
                    fields[3] = EsfNode.Leaf(EsfNodeKind.Ascii, element.SfxEvent2Stop);
                }
            }

            return RebuildRecord(record, fields);
        }

        static EsfNode BuildAttachRecord(CscElement element)
        {
            var value = EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)element.AttachBoneIndex), optimized: true);
            if (element.AttachRecord != null)
                return EsfNode.NewRecord(element.AttachRecord.Name!, element.AttachRecord.Version, element.AttachRecord.RecordFlags, [[value]]);
            return EsfNode.NewRecord("ATTACH_TO_PARAMETERS", 0, EsfRecordFlags.IsRecordNode, [[value]]);
        }

        internal static void AppendChannelGroup(List<EsfNode> fields, CscChannelGroup group)
        {
            fields.Add(EsfNode.Leaf(EsfNodeKind.U32, group.Marker, optimized: true));
            foreach (var channel in group.Channels)
            {
                fields.Add(EsfNode.Leaf(EsfNodeKind.F32, channel.Header, optimized: channel.Header == 0));
                if (channel.HasModeTags)
                {
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, channel.ModeA));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, channel.ModeB));
                }

                fields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)channel.Keyframes.Count, optimized: true));
                foreach (var key in channel.Keyframes)
                {
                    fields.Add(EsfNode.Leaf(EsfNodeKind.F32, key.Time, optimized: key.Time == 0));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.F32, key.Value, optimized: key.Value == 0));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, key.ModeIn));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Coord2d, key.TangentIn));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Coord2d, key.TangentOut));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, key.ModeOut));
                }
            }
        }

        // ---------------------------------------------------------------------
        // COMPOSITE_SCENE tail
        // ---------------------------------------------------------------------

        static List<EsfNode> RebuildTail(CscScene scene)
        {
            if (scene.ManifestKind == CscManifestKind.None)
                return scene.TailFields;

            if (!scene.ManifestParsed)
                return scene.TailFields; // nested-element (porthole) layouts etc: preserved verbatim.

            var manifestFields = BuildManifestFields(scene);

            if (scene.ManifestKind == CscManifestKind.Inline)
                return manifestFields;

            var sceneRecord = EsfNode.NewRecord(
                scene.CompositeSceneRecordName, scene.CompositeSceneVersion, scene.CompositeSceneFlags, [manifestFields]);

            var tail = new List<EsfNode>(scene.TailFields);
            tail[scene.TailSceneRecordIndex] = sceneRecord;
            return tail;
        }

        static List<EsfNode> BuildManifestFields(CscScene scene)
        {
            var dfs = scene.DfsOrder(includeNested: true);
            var fields = new List<EsfNode>
            {
                EsfNode.Leaf(EsfNodeKind.I32, dfs.Count, optimized: true),
            };

            foreach (var element in dfs)
                fields.AddRange(BuildManifestGroup(element));

            // Entry section: original entries whose elements all still exist (in original order),
            // then a fresh single-id entry for any element no entry covers.
            var liveIds = scene.AllElementsIncludingNested().Select(e => e.Id).ToHashSet();
            var entries = scene.ManifestEntries.Where(e => e.ElementIds.All(liveIds.Contains)).ToList();
            var coveredIds = entries.SelectMany(e => e.ElementIds).ToHashSet();
            foreach (var element in dfs)
            {
                if (coveredIds.Contains(element.Id))
                    continue;
                entries.Add(new CscManifestEntry
                {
                    Fields =
                    [
                        EsfNode.Leaf(EsfNodeKind.Ascii, ""),
                        EsfNode.Leaf(EsfNodeKind.Bool, false, optimized: true),
                        EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true),
                        EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true),
                        EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true),
                        EsfNode.Leaf(EsfNodeKind.I32, 1, optimized: true),
                        EsfNode.Leaf(EsfNodeKind.I32, element.Id, optimized: true),
                    ],
                    ElementIds = [element.Id],
                });
            }

            fields.Add(EsfNode.Leaf(EsfNodeKind.I32, entries.Count, optimized: true));
            foreach (var entry in entries)
                fields.AddRange(entry.Fields);

            // Footer: entry i's unit lists the entry indexes of that entry's element's attachment
            // children (single-id entries only - the corpus-confirmed adjacency reading).
            var entryIndexByElementId = new Dictionary<int, int>();
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].ElementIds.Count == 1)
                    entryIndexByElementId.TryAdd(entries[i].ElementIds[0], i);

            foreach (var entry in entries)
            {
                var childIndexes = new List<int>();
                if (entry.ElementIds.Count == 1)
                {
                    var element = scene.FindElement(entry.ElementIds[0]);
                    if (element != null)
                        foreach (var child in element.Children)
                            if (entryIndexByElementId.TryGetValue(child.Id, out var idx))
                                childIndexes.Add(idx);
                }

                fields.Add(EsfNode.Leaf(EsfNodeKind.I32, childIndexes.Count, optimized: true));
                foreach (var idx in childIndexes)
                    fields.Add(EsfNode.Leaf(EsfNodeKind.I32, idx, optimized: true));
            }

            fields.AddRange(scene.ManifestLeftoverFields);
            return fields;
        }

        /// <summary>
        /// Builds one element's manifest group. When the element was loaded with a manifest group,
        /// its channel names and flags are kept and only the keyframe counts are re-derived from
        /// the element's current channels. New elements get the unnamed (marker=0) form, which
        /// needs no channel-name knowledge - both forms are common in real files.
        /// </summary>
        static List<EsfNode> BuildManifestGroup(CscElement element)
        {
            var counts = element.AllChannelKeyframeCounts();

            if (element.ManifestGroupFields != null &&
                TryPatchManifestGroup(element.ManifestGroupFields, counts) is { } patched)
                return patched;

            // Unnamed form: U32(0) + I32(channelCount) + per group: one channel whose
            // sub-components are the group's channels.
            var groups = element.ElementGroups().Concat(element.TypeGroups).ToList();
            var fields = new List<EsfNode>
            {
                EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true),
                EsfNode.Leaf(EsfNodeKind.I32, groups.Count, optimized: true),
            };

            foreach (var group in groups)
            {
                fields.Add(EsfNode.Leaf(EsfNodeKind.I32, group.Channels.Count, optimized: true));
                foreach (var channel in group.Channels)
                {
                    fields.Add(EsfNode.Leaf(EsfNodeKind.Bool, false, optimized: true));
                    fields.Add(EsfNode.Leaf(EsfNodeKind.I32, channel.Keyframes.Count, optimized: true));
                    for (var k = 0; k < channel.Keyframes.Count; k++)
                        fields.Add(EsfNode.Leaf(EsfNodeKind.Bool, true, optimized: true));
                }
            }

            return fields;
        }

        static List<EsfNode>? TryPatchManifestGroup(List<EsfNode> originalFields, List<int> counts)
        {
            var parsed = CompositeSceneDetector.DetectGroups(originalFields);
            if (parsed.Count != 1 || parsed[0].StartIndex != 0 || parsed[0].FieldCount != originalFields.Count)
                return null;

            var group = parsed[0];
            var flatSubs = group.Channels.SelectMany(c => c.SubComponents).ToList();
            if (flatSubs.Count != counts.Count)
                return null;

            var fields = new List<EsfNode>(originalFields.Count);
            var subIndex = 0;
            var readPos = 0;

            void CopyThrough(int exclusiveEnd)
            {
                while (readPos < exclusiveEnd)
                    fields.Add(originalFields[readPos++]);
            }

            foreach (var channel in group.Channels)
            {
                foreach (var sub in channel.SubComponents)
                {
                    CopyThrough(sub.StartIndex);

                    var newCount = counts[subIndex++];
                    fields.Add(originalFields[readPos]); // Bool flag, preserved
                    fields.Add(EsfNode.Leaf(EsfNodeKind.I32, newCount, optimized: true));

                    // Per-keyframe flags: keep the originals that still apply, pad with True.
                    for (var k = 0; k < newCount; k++)
                        fields.Add(k < sub.KeyframeCount
                            ? originalFields[readPos + 2 + k]
                            : EsfNode.Leaf(EsfNodeKind.Bool, true, optimized: true));

                    readPos = sub.StartIndex + sub.FieldCount;
                }
            }

            CopyThrough(originalFields.Count);
            return fields;
        }
    }
}
