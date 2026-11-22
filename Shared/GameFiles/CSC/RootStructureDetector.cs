using Shared.GameFormats.Esf;


namespace Shared.GameFormats.Csc;

/// <summary>
/// Detects ROOT's own top-level grammar - the file's outermost record, distinct from the
/// ELEMENT/COMPOSITE_SCENE grammars covered by <see cref="CurveDetector"/> and
/// <see cref="CompositeSceneDetector"/>. Confirmed corpus-wide (all 1,462 files, zero
/// exceptions) as:
///
///   ROOT := Header(HeaderLength(version)) + U32(elementGroupCount) + elementGroupCount x ElementGroup
///           + U32(attachGroupCount) + attachGroupCount x AttachGroup + Tail
///
///   ElementGroup := Ascii(label) + one-or-more records (an ELEMENT immediately followed by
///                   zero or more type-specific records, e.g. VFX_ELEMENT, SFX_ELEMENT,
///                   ELEMENT_PERIOD - the label names the whole bundle, e.g. "vfx"/"sfx"/"group",
///                   and is shown in the UI as a synthetic grouping node)
///
///   AttachGroup := U32(parentElementId, read as a signed int32 - -1 means "no parent / top-level")
///                  + U32(childCount) + childCount x (U32(childElementId), ATTACH_TO_PARAMETERS)
///
///   Tail := a single trailing COMPOSITE_SCENE record (1,191/1,462 files), OR the same content
///           inlined directly with no record wrapper (115 files, all ROOT v1-v3 - the composite-
///           scene payload predates the wrapper record), OR nothing at all (156 files with no
///           scene content whatsoever - also older/simpler ROOT versions).
///
/// ROOT's own header length is a clean, strictly version-additive function - each version just
/// adds 3 more fields to the previous one's header, the same pattern seen in MODEL_ELEMENT/
/// VFX_ELEMENT:
///   v1 (4 files):   F32, Coord3d, F32                                                    (3 fields)
///   v2 (1 file):     + Ascii                                                              (4 fields)
///   v3 (464 files):  + Ascii                                                              (4 fields, same length as v2)
///   v4 (255 files):  + F32, Ascii, U32                                                    (7 fields)
///   v5 (738 files):  + Coord3d, Coord3d, F32                                              (10 fields)
///   v6 (329 files, newer than the counts above): + F32, Bool                              (12 fields)
///     The two new fields' meaning is unknown: the F32 is 1 and the Bool false in 313 of 329 vanilla
///     files (0.75/true and 1/true in 8 each). They are kept as read; the field kinds are checked so a
///     later layout isn't mistaken for this one.
///
/// ElementGroup's ELEMENT record carries its own leading U32 as a per-ROOT element id (field
/// index 0, all versions) - confirmed by cross-referencing the attachment tree below, where
/// every id referenced as a parent or child exactly matches some ELEMENT's own id field, and by
/// COMPOSITE_SCENE's manifest walking every element in the same encounter order.
///
/// The attachment tree was reverse-engineered by finding the AttachGroup boundary programmatically:
/// with ROOT's header length and the element/attach grammar both fixed, the leading U32 before
/// each group of (parentId, childCount) is forced to be exactly the count of remaining groups -
/// confirmed by summing each group's childCount and checking it equals the file's total ELEMENT
/// count, corpus-wide (11,546/11,546 - every element appears as exactly one attachment child).
///
/// ATTACH_TO_PARAMETERS's own U32 field is NOT a redundant copy of the parent id (a natural first
/// guess, since it visually sits right next to the child id) - measured corpus-wide across 22,051
/// records:
///   - when parentId == -1 (top-level, no real parent): the value is -1 100% of the time (4,856/4,856)
///   - when parentId is a real element id: the value is still -1 92.8% of the time (15,953/17,195),
///     matches the parent id only 0.12% of the time (21/17,195 - likely coincidence, not a real
///     correlation), and is some other small value the rest of the time (1,221/17,195, 7.1%) - often
///     small numbers suggestive of a bone/socket index within the parent (e.g. wh2_dlc13_kalara_firing.csc,
///     a turret rig, shows values like 0/1/5/6 repeated across multiple children of the same
///     parent). Left as an open, separately-tracked field rather than folded into the attachment
///     tree it sits inside.
///
/// All three of parentId/childElementId/attachValue are exposed here as signed <c>int</c>, not the
/// <c>uint</c> the wire encoding (<see cref="EsfNodeKind.U32"/>) literally carries: the "no parent"
/// sentinel is far more naturally read as a plain <c>-1</c> than as <c>4294967295</c>, and every
/// real id seen in the corpus is a small non-negative number that fits either representation
/// identically - so nothing is lost, and the sentinel case reads correctly.
/// </summary>
public static class RootStructureDetector
{
    private static readonly Dictionary<byte, int> HeaderLengthByVersion = new()
    {
        [1] = 3,
        [2] = 4,
        [3] = 4,
        [4] = 7,
        [5] = 10,
        [6] = 12,
    };

    private static readonly EsfNodeKind[] Version6HeaderKinds =
    [
        EsfNodeKind.F32, EsfNodeKind.Coord3d, EsfNodeKind.F32, EsfNodeKind.Ascii,
        EsfNodeKind.F32, EsfNodeKind.Ascii, EsfNodeKind.U32, EsfNodeKind.Coord3d,
        EsfNodeKind.Coord3d, EsfNodeKind.F32, EsfNodeKind.F32, EsfNodeKind.Bool,
    ];

    public sealed record ElementGroupInfo(int StartIndex, int FieldCount, string Label, IReadOnlyList<EsfNode> Records);

    public sealed record AttachChildInfo(int StartIndex, int ElementId, int AttachValue);

    public sealed record AttachGroupInfo(int StartIndex, int FieldCount, int ParentId, IReadOnlyList<AttachChildInfo> Children);

    public sealed record StructureInfo(
        int HeaderLength,
        IReadOnlyList<ElementGroupInfo> ElementGroups,
        int AttachSectionStart,
        IReadOnlyList<AttachGroupInfo> AttachGroups,
        int TailStartIndex);

    /// <summary>
    /// Attempts to detect ROOT's full structure in <paramref name="siblings"/> (ROOT's own
    /// flattened children). Returns null if the version's header length is unknown, or if the
    /// grammar doesn't hold - e.g. a not-yet-seen ROOT version - so callers can fall back to
    /// showing raw fields rather than presenting a wrong grouping as fact.
    /// </summary>
    public static StructureInfo? Detect(IReadOnlyList<EsfNode> siblings, byte version)
    {
        if (!HeaderLengthByVersion.TryGetValue(version, out var headerLength))
            return null;

        if (version == 6 && !siblings.Take(headerLength).Select(field => field.Kind).SequenceEqual(Version6HeaderKinds))
            return null;

        var i = headerLength;
        if (i >= siblings.Count || siblings[i].Kind != EsfNodeKind.U32 || siblings[i].Value is not uint elementGroupCount)
            return null;
        i++;

        var elementGroups = new List<ElementGroupInfo>((int)Math.Min(elementGroupCount, int.MaxValue));
        var elementIds = new List<uint>();
        for (var g = 0; g < elementGroupCount; g++)
        {
            if (i >= siblings.Count || siblings[i].Kind != EsfNodeKind.Ascii || siblings[i].Value is not string label)
                return null;
            var groupStart = i;
            i++;

            var records = new List<EsfNode>();
            while (i < siblings.Count && siblings[i].IsRecord)
            {
                var record = siblings[i];
                if (record.Name == "ELEMENT")
                {
                    var fields = record.Children.ToList();
                    if (fields.Count > 0 && fields[0].Kind == EsfNodeKind.U32 && fields[0].Value is uint elementId)
                        elementIds.Add(elementId);
                }
                records.Add(record);
                i++;
            }
            if (records.Count == 0)
                return null;

            elementGroups.Add(new ElementGroupInfo(groupStart, i - groupStart, label, records));
        }

        var attachSectionStart = i;
        if (i >= siblings.Count || siblings[i].Kind != EsfNodeKind.U32 || siblings[i].Value is not uint attachGroupCount)
            return null;
        i++;

        var attachGroups = new List<AttachGroupInfo>((int)Math.Min(attachGroupCount, int.MaxValue));
        var totalChildren = 0L;
        for (var g = 0; g < attachGroupCount; g++)
        {
            if (i + 1 >= siblings.Count || siblings[i].Kind != EsfNodeKind.U32 || siblings[i + 1].Kind != EsfNodeKind.U32)
                return null;
            if (siblings[i].Value is not uint parentIdRaw || siblings[i + 1].Value is not uint childCount)
                return null;
            var parentId = (int)parentIdRaw;

            var groupStart = i;
            i += 2;

            var children = new List<AttachChildInfo>((int)Math.Min(childCount, int.MaxValue));
            for (var c = 0; c < childCount; c++)
            {
                if (i + 1 >= siblings.Count || siblings[i].Kind != EsfNodeKind.U32 ||
                    siblings[i + 1] is not { IsRecord: true, Name: "ATTACH_TO_PARAMETERS" } attachRecord)
                    return null;
                if (siblings[i].Value is not uint childElementIdRaw)
                    return null;
                var childElementId = (int)childElementIdRaw;

                var attachFields = attachRecord.Children.ToList();
                var attachValue = attachFields.Count == 1 && attachFields[0].Kind == EsfNodeKind.U32 && attachFields[0].Value is uint av
                    ? (int)av
                    : -1;

                children.Add(new AttachChildInfo(i, childElementId, attachValue));
                i += 2;
            }

            attachGroups.Add(new AttachGroupInfo(groupStart, i - groupStart, parentId, children));
            totalChildren += childCount;
        }

        if (totalChildren != elementIds.Count)
            return null;

        return new StructureInfo(headerLength, elementGroups, attachSectionStart, attachGroups, i);
    }
}
