namespace Shared.Core.PackFiles.Utility
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

    public class FileSystemWatcherWrapper : IFileSystemWatcher
    {
        private readonly FileSystemWatcher _watcher;

        public FileSystemWatcherWrapper()
        {
            _watcher = new FileSystemWatcher();
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _watcher.Created += (s, e) => Created?.Invoke(s, e);
            _watcher.Deleted += (s, e) => Deleted?.Invoke(s, e);
            _watcher.Renamed += (s, e) => Renamed?.Invoke(s, e);
        }

        public string Path { get => _watcher.Path; set => _watcher.Path = value; }
        public bool IncludeSubdirectories { get => _watcher.IncludeSubdirectories; set => _watcher.IncludeSubdirectories = value; }
        public bool EnableRaisingEvents { get => _watcher.EnableRaisingEvents; set => _watcher.EnableRaisingEvents = value; }

        public event FileSystemEventHandler? Created;
        public event FileSystemEventHandler? Deleted;
        public event RenamedEventHandler? Renamed;

        public void Dispose() => _watcher.Dispose();
    }
}
