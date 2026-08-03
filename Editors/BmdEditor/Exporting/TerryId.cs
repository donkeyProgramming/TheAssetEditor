namespace Editors.BmdEditor.Exporting
{
    /// <summary>
    /// Generates Terry-style entity/project ids: lowercase hex, no leading zeros (real ids read like
    /// a plain hex integer - e.g. timestamp/counter derived - never zero-padded), matching the shape
    /// seen in real .terry/.layer files. Real Terry ids aren't random - they look derived from a
    /// timestamp/counter (ids created in the same editing session share a long common prefix) -
    /// but the exact proprietary scheme isn't known here, so this uses .NET's own canonical unique
    /// id primitive (a GUID) rather than a hand-rolled random loop. Terry only requires ids to be
    /// unique within a project, so this is sufficient even though it won't reproduce that shared-prefix
    /// shape.
    /// </summary>
    public static class TerryId
    {
        public static string NewId()
        {
            var id = Guid.NewGuid().ToString("N")[..15].TrimStart('0');
            return id.Length > 0 ? id : "0";
        }
    }
}
