using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.AudioProject;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioSettingsViewModel : ObservableObject, IEditorInterface
    {
        //readonly ILogger _logger = Logging.Create<AudioSettingsViewModel>();
        private readonly IPackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Settings";

        [ObservableProperty] private PlayType _selectedPlayType;
        [ObservableProperty] private ObservableCollection<PlayType> _playTypes = new(Enum.GetValues<PlayType>());

        public AudioSettingsViewModel(IPackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;
        }

        public void Close()
        {
        }

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
