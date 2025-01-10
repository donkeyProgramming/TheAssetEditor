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

        public static AudioProject.AudioSettings BuildAudioSettings(AudioSettingsViewModel audioSettingsViewModel, int audioFilesCount)
        {
            var audioSettings = new AudioProject.AudioSettings();

            if (audioSettingsViewModel.InitialDelay > 0)
                audioSettings.InitialDelay = audioSettingsViewModel.InitialDelay;

            if (audioSettingsViewModel.Volume > 0)
                audioSettings.Volume = audioSettingsViewModel.Volume;

            if (audioFilesCount > 1)
            {
                audioSettings.PlaylistType = audioSettingsViewModel.PlaylistType;

                audioSettings.PlaylistMode = audioSettingsViewModel.PlaylistMode;
                audioSettings.RepetitionInterval = audioSettingsViewModel.RepetitionInterval;
                audioSettings.EndBehaviour = audioSettingsViewModel.EndBehaviour;

                if (audioSettingsViewModel.IsLoopingEnabled)
                {
                    audioSettings.IsLoopingEnabled = audioSettingsViewModel.IsLoopingEnabled;
                    if (audioSettingsViewModel.IsLoopingInfinitely)
                        audioSettings.IsLoopingInfinitely = audioSettingsViewModel.IsLoopingInfinitely;
                    else
                        audioSettings.NumberOfLoops = audioSettingsViewModel.NumberOfLoops;
                }

                if (audioSettingsViewModel.IsTransitionsEnabled)
                {
                    audioSettings.IsTransitionsEnabled = audioSettingsViewModel.IsTransitionsEnabled;
                    audioSettings.Transition = audioSettingsViewModel.Transition;
                    audioSettings.Duration = audioSettingsViewModel.Duration;
                }
            }

            return audioSettings;
        }

        public void Close()
        {
        }
    }
}
