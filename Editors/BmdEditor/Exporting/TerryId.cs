namespace Editors.BmdEditor.Exporting
{
    /// <summary>
    /// Generates entity/project ids shaped like Terry's: lowercase hex with no leading zeros. Terry's own
    /// ids look timestamp/counter derived, but it only needs them unique within a project, so a GUID does.
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
