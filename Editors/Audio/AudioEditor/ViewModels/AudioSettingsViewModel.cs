using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioSettingsViewModel : ObservableObject, IEditorInterface
    {
        //readonly ILogger _logger = Logging.Create<AudioSettingsViewModel>();

        public string DisplayName { get; set; } = "Audio Settings";

        // General
        [ObservableProperty] private PlaylistType _playlistType;
        [ObservableProperty] private ObservableCollection<PlaylistType> _playlistTypes = new(Enum.GetValues<PlaylistType>());
        [ObservableProperty] private decimal _volume;
        [ObservableProperty] private decimal _initialDelay = 0;

        // Playlist
        [ObservableProperty] private PlaylistMode _playlistMode;
        [ObservableProperty] private ObservableCollection<PlaylistMode> _playlistModes = new(Enum.GetValues<PlaylistMode>());
        [ObservableProperty] private uint _repetitionInterval = 1;
        [ObservableProperty] private EndBehaviour _endBehaviour;
        [ObservableProperty] private ObservableCollection<EndBehaviour> _endBehaviours = new(Enum.GetValues<EndBehaviour>());

        // Playlist Loopini
        [ObservableProperty] private bool _isLoopingEnabled;
        [ObservableProperty] private bool _isLoopingInfinitely;
        [ObservableProperty] private uint _numberOfLoops = 1;

        // Playlist Transitions
        [ObservableProperty] private bool _isTransitionsEnabled;
        [ObservableProperty] private Transition _transition;
        [ObservableProperty] private ObservableCollection<Transition> _transitions = new(Enum.GetValues<Transition>());
        [ObservableProperty] private decimal _duration;

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
