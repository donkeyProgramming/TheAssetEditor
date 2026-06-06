namespace Shared.Core.Services
{
    public interface IFileSystemWatcher : IDisposable
    {
        string Path { get; set; }
        bool IncludeSubdirectories { get; set; }
        bool EnableRaisingEvents { get; set; }

        event FileSystemEventHandler? Created;
        event FileSystemEventHandler? Deleted;
        event RenamedEventHandler? Renamed;
    }
}
