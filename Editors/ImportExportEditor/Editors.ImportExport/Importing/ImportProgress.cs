namespace Editors.ImportExport.Importing
{
    /// <summary>
    /// One progress report from an importer. Total &lt;= 0 means the step has no known length.
    /// </summary>
    public sealed record ImportProgress(
        string Status,
        string? Detail = null,
        long Completed = 0,
        long Total = 0);
}
