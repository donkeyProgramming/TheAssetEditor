namespace Shared.Core.PackFiles.Models
{
    public interface IPackFileContainer
    {
        string Name { get; }
        bool IsCaPackFile { get; set; }
        string SystemFilePath { get; }
        Dictionary<string, PackFile> FileList { get; }
        HashSet<string> SourcePackFilePaths { get; }
    }
}
