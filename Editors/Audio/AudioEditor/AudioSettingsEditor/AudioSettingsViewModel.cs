using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettingsEditor.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioSettingsEditor
{
    public partial class AudioSettingsEditorViewModel : ObservableObject, IEditorInterface
    {
        //readonly ILogger _logger = Logging.Create<AudioSettingsViewModel>();

        public string DisplayName { get; set; } = "Audio Settings";
        [ObservableProperty] private bool _isAudioSettingsVisible = false;

        // General
        [ObservableProperty] private decimal _volume;
        [ObservableProperty] private decimal _initialDelay = 0;

        // Playlist
        [ObservableProperty] private PlaylistType _playlistType;
        [ObservableProperty] private ObservableCollection<PlaylistType> _playlistTypes = new(Enum.GetValues<PlaylistType>());
        [ObservableProperty] private bool _isPlaylistTypeEnabled = false;
        [ObservableProperty] private PlaylistMode _playlistMode;
        [ObservableProperty] private ObservableCollection<PlaylistMode> _playlistModes = new(Enum.GetValues<PlaylistMode>());
        [ObservableProperty] private bool _isPlaylistModeEnabled = false;
        [ObservableProperty] private bool _enableRepetitionInterval = false;
        [ObservableProperty] private bool _isEnableRepetitionIntervalEnabled = false;
        [ObservableProperty] private uint _repetitionInterval = 1;
        [ObservableProperty] private bool _isRepetitionIntervalEnabled = false;
        [ObservableProperty] private EndBehaviour _endBehaviour;
        [ObservableProperty] private ObservableCollection<EndBehaviour> _endBehaviours = new(Enum.GetValues<EndBehaviour>());
        [ObservableProperty] private bool _isEndBehaviourEnabled = false;

        // Playlist Looping
        [ObservableProperty] private bool _enableLooping;
        [ObservableProperty] private bool _isEnableLoopingEnabled = false;
        [ObservableProperty] private bool _loopInfinitely;
        [ObservableProperty] private bool _isLoopInfinitelyEnabled = false;
        [ObservableProperty] private uint _numberOfLoops;
        [ObservableProperty] private bool _isNumberOfLoopsEnabled = false;

        // Playlist Transitions
        [ObservableProperty] private bool _enableTransitions;
        [ObservableProperty] private bool _isEnableTransitionsEnabled = false;
        [ObservableProperty] private TransitionType _transition;
        [ObservableProperty] private ObservableCollection<TransitionType> _transitions = new(Enum.GetValues<TransitionType>());
        [ObservableProperty] private bool _isTransitionTypeEnabled = false;
        [ObservableProperty] private decimal _duration;
        [ObservableProperty] private bool _isDurationEnabled = false;

        public bool IsUsingMultipleAudioFiles { get; set; }

        partial void OnPlaylistTypeChanged(PlaylistType oldValue, PlaylistType newValue)
        {
            SetAudioSettingsEnablement(this);
        }

        partial void OnPlaylistModeChanged(PlaylistMode oldValue, PlaylistMode newValue)
        {
            SetAudioSettingsEnablement(this);
        }

        partial void OnEnableLoopingChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablement(this);
        }

        partial void OnLoopInfinitelyChanged(bool oldValue, bool newValue)
        {
            UpdateLoopingEnablement(this);
        }

        partial void OnNumberOfLoopsChanged(uint oldValue, uint newValue)
        {
            UpdateLoopingEnablement(this);
        }

        partial void OnEnableTransitionsChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablement(this);
        }

        public static Data.AudioSettings BuildAudioSettings(AudioSettingsEditorViewModel audioSettingsViewModel)
        {
            var audioSettings = new Data.AudioSettings();

            if (audioSettingsViewModel.InitialDelay > 0)
                audioSettings.InitialDelay = audioSettingsViewModel.InitialDelay;

            if (audioSettingsViewModel.Volume > 0)
                audioSettings.Volume = audioSettingsViewModel.Volume;

            if (audioSettingsViewModel.IsUsingMultipleAudioFiles)
            {
                audioSettings.PlaylistType = audioSettingsViewModel.PlaylistType;
                audioSettings.PlaylistMode = audioSettingsViewModel.PlaylistMode;

                if (audioSettingsViewModel.PlaylistType == PlaylistType.Sequence)
                    audioSettings.EndBehaviour = audioSettingsViewModel.EndBehaviour;
                else
                {
                    audioSettings.EnableRepetitionInterval = audioSettingsViewModel.EnableRepetitionInterval;
                    if (audioSettingsViewModel.EnableRepetitionInterval)
                        audioSettings.RepetitionInterval = audioSettingsViewModel.RepetitionInterval;
                }

                if (audioSettingsViewModel.EnableLooping)
                {
                    audioSettings.EnableLooping = audioSettingsViewModel.EnableLooping;
                    if (audioSettingsViewModel.LoopInfinitely)
                        audioSettings.ILoopInfinitely = audioSettingsViewModel.LoopInfinitely;
                    else
                        audioSettings.NumberOfLoops = audioSettingsViewModel.NumberOfLoops;
                }

                if (audioSettingsViewModel.EnableTransitions)
                {
                    audioSettings.EnableTransitions = audioSettingsViewModel.EnableTransitions;
                    audioSettings.Transition = audioSettingsViewModel.Transition;
                    audioSettings.Duration = audioSettingsViewModel.Duration;
                }
            }

            return audioSettings;
        }

        public static void SetAudioSettingsEnablement(AudioSettingsEditorViewModel audioSettingsViewModel)
        {
            audioSettingsViewModel.IsAudioSettingsVisible = true;

            if (audioSettingsViewModel.IsUsingMultipleAudioFiles)
            {
                audioSettingsViewModel.IsPlaylistTypeEnabled = true;
                audioSettingsViewModel.IsPlaylistModeEnabled = true;

                if (audioSettingsViewModel.PlaylistType == PlaylistType.Sequence)
                {
                    audioSettingsViewModel.IsEndBehaviourEnabled = true;
                    audioSettingsViewModel.IsEnableRepetitionIntervalEnabled = false;
                    audioSettingsViewModel.IsRepetitionIntervalEnabled = false;
                }
                else
                {
                    audioSettingsViewModel.IsEnableRepetitionIntervalEnabled = true;
                    audioSettingsViewModel.IsRepetitionIntervalEnabled = true;
                    audioSettingsViewModel.IsEndBehaviourEnabled = false;
                }

                if (audioSettingsViewModel.PlaylistMode == PlaylistMode.Continuous)
                {
                    audioSettingsViewModel.IsEnableLoopingEnabled = true;
                    if (audioSettingsViewModel.EnableLooping)
                    {
                        audioSettingsViewModel.IsLoopInfinitelyEnabled = true;
                        audioSettingsViewModel.IsNumberOfLoopsEnabled = true;
                    }
                    else
                    {
                        audioSettingsViewModel.IsLoopInfinitelyEnabled = false;
                        audioSettingsViewModel.IsNumberOfLoopsEnabled = false;
                    }

                    audioSettingsViewModel.IsEnableTransitionsEnabled = true;
                    if (audioSettingsViewModel.EnableTransitions)
                    {
                        audioSettingsViewModel.IsTransitionTypeEnabled = true;
                        audioSettingsViewModel.IsDurationEnabled = true;
                    }
                    else
                    {
                        audioSettingsViewModel.IsTransitionTypeEnabled = false;
                        audioSettingsViewModel.IsDurationEnabled = false;
                    }
                }
                else
                {
                    audioSettingsViewModel.IsEnableLoopingEnabled = false;
                    audioSettingsViewModel.IsLoopInfinitelyEnabled = false;
                    audioSettingsViewModel.IsNumberOfLoopsEnabled = false;

                    audioSettingsViewModel.IsEnableTransitionsEnabled = false;
                    audioSettingsViewModel.IsTransitionTypeEnabled = false;
                    audioSettingsViewModel.IsDurationEnabled = false;
                }
            }
            else
            {
                audioSettingsViewModel.IsPlaylistTypeEnabled = false;
                audioSettingsViewModel.IsPlaylistModeEnabled = false;
                audioSettingsViewModel.IsEndBehaviourEnabled = false;
                audioSettingsViewModel.IsEnableRepetitionIntervalEnabled = false;
                audioSettingsViewModel.IsRepetitionIntervalEnabled = false;
                audioSettingsViewModel.IsEnableLoopingEnabled = false;
                audioSettingsViewModel.IsLoopInfinitelyEnabled = false;
                audioSettingsViewModel.IsNumberOfLoopsEnabled = false;
                audioSettingsViewModel.IsEnableTransitionsEnabled = false;
                audioSettingsViewModel.IsTransitionTypeEnabled = false;
                audioSettingsViewModel.IsDurationEnabled = false;
            }
        }

        public static void UpdateLoopingEnablement(AudioSettingsEditorViewModel audioSettingsViewModel)
        {
            if (audioSettingsViewModel.IsUsingMultipleAudioFiles)
            {
                if (audioSettingsViewModel.PlaylistMode == PlaylistMode.Continuous)
                {
                    if (audioSettingsViewModel.EnableLooping)
                    {
                        if (audioSettingsViewModel.LoopInfinitely)
                        {
                            audioSettingsViewModel.IsNumberOfLoopsEnabled = false;
                        }
                        else
                        {
                            audioSettingsViewModel.IsNumberOfLoopsEnabled = true;
                        }

                        if (audioSettingsViewModel.NumberOfLoops > 0 && !audioSettingsViewModel.LoopInfinitely)
                        {
                            audioSettingsViewModel.LoopInfinitely = false;
                        }
                    }
                }
            }
        }

        public void Close()
        {
        }
    }
}
