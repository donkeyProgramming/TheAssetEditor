using System.IO;

namespace CommonControls.Common
{
    public delegate void FileChangedDelegate(string path);
    public delegate void FileDeletedDelegate(string path);

    public interface IFilePath
    {
        string Path { get; set; }
    }

    public class FilePath:IFilePath
    {
        public FilePath(string path)
        {
            Path = path;
        }
        public string Path { get; set; }
    }

    public class WatchedFilePath:IFilePath
    {
        private string _path;
        private readonly FileSystemWatcher _watcher;

        public event FileChangedDelegate FileChanged;
        public event FileDeletedDelegate FileDeleted;

        public WatchedFilePath(string path)
        {
            _path = path;

            var watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(_path));
            watcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size;
            watcher.Changed += WatcherOnChanged;
            watcher.Deleted += WatcherOnDeleted;
            watcher.Filter = System.IO.Path.GetFileName(_path);
            watcher.EnableRaisingEvents = true;

            _watcher = watcher;
        }

        ~WatchedFilePath()
        {
            _watcher.Changed -= WatcherOnChanged;
            _watcher.Deleted -= WatcherOnDeleted;
            _watcher.Dispose();
        }


        private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            FileDeleted?.Invoke(_path);
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            FileChanged?.Invoke(_path);
        }

        public string Path
        {
            get => _path;
            set => _path = value;
        }
    }
}
