using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public class PackFileSettings
    {
        private string? _saveLocationPath;
        private GameTypeEnum? _gameVersion;
        private ObservableCollection<string> _ignoredFilesWhenSerializing = new();

        public event Action? SettingsChanged;

        public PackFileSettings()
        {
            _ignoredFilesWhenSerializing.CollectionChanged += OnIgnoredFilesChanged;
        }

        public string? SaveLocationPath
        {
            get => _saveLocationPath;
            set
            {
                if (_saveLocationPath == value)
                    return;

                _saveLocationPath = value;
                SettingsChanged?.Invoke();
            }
        }

        public GameTypeEnum? GameVersion
        {
            get => _gameVersion;
            set
            {
                if (_gameVersion == value)
                    return;

                _gameVersion = value;
                SettingsChanged?.Invoke();
            }
        }

        public ObservableCollection<string> IgnoredFilesWhenSerializing
        {
            get => _ignoredFilesWhenSerializing;
            set
            {
                if (ReferenceEquals(_ignoredFilesWhenSerializing, value))
                    return;

                _ignoredFilesWhenSerializing.CollectionChanged -= OnIgnoredFilesChanged;
                _ignoredFilesWhenSerializing = value ?? new ObservableCollection<string>();
                _ignoredFilesWhenSerializing.CollectionChanged += OnIgnoredFilesChanged;
                SettingsChanged?.Invoke();
            }
        }

        private void OnIgnoredFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SettingsChanged?.Invoke();
        }
    }
}