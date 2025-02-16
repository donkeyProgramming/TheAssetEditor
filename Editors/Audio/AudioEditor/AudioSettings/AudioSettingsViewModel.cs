using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    public partial class AudioSettingsViewModel : ObservableObject, IEditorInterface
    {
        public string DisplayName { get; set; } = "Audio Settings";

        // General
        [ObservableProperty] private decimal? _volume;
        [ObservableProperty] private bool _isVolumeEnabled = true;
        [ObservableProperty] private decimal? _initialDelay;
        [ObservableProperty] private bool _isInitialDelayEnabled = true;

        // Playlist
        [ObservableProperty] private PlaylistType? _playlistType;
        [ObservableProperty] private ObservableCollection<PlaylistType> _playlistTypes = new(Enum.GetValues<PlaylistType>());
        [ObservableProperty] private bool _isPlaylistTypeEnabled = false;
        [ObservableProperty] private PlaylistMode? _playlistMode;
        [ObservableProperty] private ObservableCollection<PlaylistMode> _playlistModes = new(Enum.GetValues<PlaylistMode>());
        [ObservableProperty] private bool _isPlaylistModeEnabled = false;
        [ObservableProperty] private bool _enableRepetitionInterval = false;
        [ObservableProperty] private bool _isEnableRepetitionIntervalEnabled = false;
        [ObservableProperty] private uint? _repetitionInterval;
        [ObservableProperty] private bool _isRepetitionIntervalEnabled = false;
        [ObservableProperty] private EndBehaviour? _endBehaviour;
        [ObservableProperty] private ObservableCollection<EndBehaviour> _endBehaviours = new(Enum.GetValues<EndBehaviour>());
        [ObservableProperty] private bool _isEndBehaviourEnabled = false;

        // Playlist Looping
        [ObservableProperty] private bool _enableLooping;
        [ObservableProperty] private bool _isEnableLoopingEnabled = false;
        [ObservableProperty] private bool _loopInfinitely;
        [ObservableProperty] private bool _isLoopInfinitelyEnabled = false;
        [ObservableProperty] private uint? _numberOfLoops;
        [ObservableProperty] private bool _isNumberOfLoopsEnabled = false;

        // Playlist Transitions
        [ObservableProperty] private bool _enableTransitions;
        [ObservableProperty] private bool _isEnableTransitionsEnabled = false;
        [ObservableProperty] private TransitionType? _transition;
        [ObservableProperty] private ObservableCollection<TransitionType> _transitions = new(Enum.GetValues<TransitionType>());
        [ObservableProperty] private bool _isTransitionTypeEnabled = false;
        [ObservableProperty] private decimal? _duration;
        [ObservableProperty] private bool _isDurationEnabled = false;

        [ObservableProperty] private bool _isAudioSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        public ObservableCollection<AudioFile> AudioFiles { get; set; } = [];

        public AudioSettingsViewModel()
        {
            SetInitialAudioSettings();
        }

        partial void OnShowSettingsFromAudioProjectViewerChanged(bool oldValue, bool newValue)
        {
            if (ShowSettingsFromAudioProjectViewer == false)
            {
                SetInitialAudioSettings();
                SetAudioSettingsEnablementAndVisibility();
            }
        }

        partial void OnPlaylistTypeChanged(PlaylistType? oldValue, PlaylistType? newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnPlaylistModeChanged(PlaylistMode? oldValue, PlaylistMode? newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnEnableRepetitionIntervalChanged(bool oldValue, bool newValue)
        {
            if (EnableRepetitionInterval)
            {
                if (RepetitionInterval == null)
                    RepetitionInterval = 1;
            }
        }

        partial void OnEnableLoopingChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnLoopInfinitelyChanged(bool oldValue, bool newValue)
        {
            UpdateLoopingEnablement();
        }

        partial void OnNumberOfLoopsChanged(uint? oldValue, uint? newValue)
        {
            UpdateLoopingEnablement();
        }

        partial void OnEnableTransitionsChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        public void SetAudioFiles(List<AudioFile> audioFiles)
        {
            foreach (var audioFile in audioFiles)
                AudioFiles.Add(audioFile);
        }

        public Data.AudioSettings BuildAudioSettings()
        {
            var audioSettings = new Data.AudioSettings();

            audioSettings.InitialDelay = InitialDelay;
            audioSettings.Volume = Volume;

            if (AudioFiles.Count > 1) // This should prevent the recording of any settings stored but not in use e.g. from when the user has previously applied settings with multiple sounds set
            {
                audioSettings.PlaylistType = PlaylistType;
                audioSettings.PlaylistMode = PlaylistMode;

                if (PlaylistType == AudioSettings.PlaylistType.Sequence)
                    audioSettings.EndBehaviour = EndBehaviour;
                else
                {
                    audioSettings.EnableRepetitionInterval = EnableRepetitionInterval;
                    if (EnableRepetitionInterval)
                        audioSettings.RepetitionInterval = RepetitionInterval;
                }

                if (EnableLooping)
                {
                    audioSettings.EnableLooping = EnableLooping;
                    if (LoopInfinitely)
                        audioSettings.LoopInfinitely = LoopInfinitely;
                    else
                        audioSettings.NumberOfLoops = NumberOfLoops;
                }

                if (EnableTransitions)
                {
                    audioSettings.EnableTransitions = EnableTransitions;
                    audioSettings.Transition = Transition;
                    audioSettings.Duration = Duration;
                }
            }

            return audioSettings;
        }

        public void SetAudioSettingsFromAudioProjectItemAudioSettings(Data.AudioSettings audioSettings, int audioFilesCount)
        {
            ResetAudioSettingsGeneral();
            ResetAudioSettingsPlaylist();
            ResetAudioSettingsPlaylistLooping();
            ResetAudioSettingsTransitions();
            ResetAudioFiles();

            InitialDelay = (decimal)audioSettings.InitialDelay;
            Volume = (decimal)audioSettings.Volume;

            if (audioFilesCount > 1)
            {
                PlaylistType = (PlaylistType)audioSettings.PlaylistType;
                PlaylistMode = (PlaylistMode)audioSettings.PlaylistMode;

                if (audioSettings.PlaylistType == AudioSettings.PlaylistType.Sequence)
                    EndBehaviour = (EndBehaviour)audioSettings.EndBehaviour;
                else
                {
                    EnableRepetitionInterval = (bool)audioSettings.EnableRepetitionInterval;
                    if (EnableRepetitionInterval)
                        RepetitionInterval = (uint)audioSettings.RepetitionInterval;
                }

                if (audioSettings.EnableLooping.HasValue && (bool)audioSettings.EnableLooping)
                {
                    EnableLooping = (bool)audioSettings.EnableLooping;
                    if (LoopInfinitely)
                        LoopInfinitely = (bool)audioSettings.LoopInfinitely;
                    else
                        NumberOfLoops = (uint)audioSettings.NumberOfLoops;
                }

                if (audioSettings.EnableTransitions.HasValue && (bool)audioSettings.EnableTransitions)
                {
                    EnableTransitions = (bool)audioSettings.EnableTransitions;
                    Transition = (TransitionType)audioSettings.Transition;
                    Duration = (decimal)audioSettings.Duration;
                }
            }

            SetAudioSettingsEnablementAndVisibility();
        }

        public void SetInitialAudioSettings()
        {
            Volume = null;
            InitialDelay = null;
            PlaylistType = AudioSettings.PlaylistType.RandomExhaustive;
            PlaylistMode = AudioSettings.PlaylistMode.Step;
            RepetitionInterval = null;
            EndBehaviour = AudioSettings.EndBehaviour.Restart;
            NumberOfLoops = null;
            Transition = null;
            Duration = null;
        }

        public void SetAudioSettingsEnablementAndVisibility()
        {
            IsAudioSettingsVisible = true;
            IsVolumeEnabled = true;
            IsInitialDelayEnabled = true;

            if (AudioFiles.Count > 1)
            {
                IsPlaylistTypeEnabled = true;
                IsPlaylistModeEnabled = true;

                if (PlaylistType == AudioSettings.PlaylistType.Sequence)
                {
                    IsEndBehaviourEnabled = true;
                    IsEnableRepetitionIntervalEnabled = false;
                    IsRepetitionIntervalEnabled = false;
                }
                else
                {
                    IsEnableRepetitionIntervalEnabled = true;
                    IsRepetitionIntervalEnabled = true;
                    IsEndBehaviourEnabled = false;
                }

                if (PlaylistMode == AudioSettings.PlaylistMode.Continuous)
                {
                    IsEnableLoopingEnabled = true;
                    if (EnableLooping)
                    {
                        IsLoopInfinitelyEnabled = true;
                        IsNumberOfLoopsEnabled = true;
                    }
                    else
                    {
                        IsLoopInfinitelyEnabled = false;
                        IsNumberOfLoopsEnabled = false;
                    }

                    IsEnableTransitionsEnabled = true;
                    if (EnableTransitions)
                    {
                        IsTransitionTypeEnabled = true;
                        IsDurationEnabled = true;
                    }
                    else
                    {
                        IsTransitionTypeEnabled = false;
                        IsDurationEnabled = false;
                    }
                }
                else
                {
                    IsEnableLoopingEnabled = false;
                    IsLoopInfinitelyEnabled = false;
                    IsNumberOfLoopsEnabled = false;

                    IsEnableTransitionsEnabled = false;
                    IsTransitionTypeEnabled = false;
                    IsDurationEnabled = false;
                }
            }
            else
            {
                IsPlaylistTypeEnabled = false;
                IsPlaylistModeEnabled = false;
                IsEndBehaviourEnabled = false;
                IsEnableRepetitionIntervalEnabled = false;
                IsRepetitionIntervalEnabled = false;
                IsEnableLoopingEnabled = false;
                IsLoopInfinitelyEnabled = false;
                IsNumberOfLoopsEnabled = false;
                IsEnableTransitionsEnabled = false;
                IsTransitionTypeEnabled = false;
                IsDurationEnabled = false;
            }
        }

        public void UpdateLoopingEnablement()
        {
            if (AudioFiles.Count > 1)
            {
                if (PlaylistMode == AudioSettings.PlaylistMode.Continuous)
                {
                    if (EnableLooping)
                    {
                        if (LoopInfinitely)
                            IsNumberOfLoopsEnabled = false;
                        else
                            IsNumberOfLoopsEnabled = true;

                        if (NumberOfLoops > 0 && !LoopInfinitely)
                            LoopInfinitely = false;
                    }
                }
            }
        }

        [RelayCommand]
        public void SetRecommendedAudioSettings()
        {
            Volume = 0;
            InitialDelay = 0;

            if (AudioFiles.Count > 1)
            {
                PlaylistType = AudioSettings.PlaylistType.RandomExhaustive;
                PlaylistMode = AudioSettings.PlaylistMode.Step;
                EnableRepetitionInterval = true;
                RepetitionInterval = (uint)Math.Ceiling(AudioFiles.Count / 2.0);
                EndBehaviour = AudioSettings.EndBehaviour.Restart;

                EnableLooping = false;
                LoopInfinitely = false;
                NumberOfLoops = 0;

                EnableTransitions = false;
                Transition = TransitionType.XfadeAmp;
                Duration = 0;
            }
        }

        public void DisableAllAudioSettings()
        {
            IsVolumeEnabled = false;
            IsInitialDelayEnabled = false;
            IsPlaylistTypeEnabled = false;
            IsPlaylistModeEnabled = false;
            IsEnableRepetitionIntervalEnabled = false;
            IsRepetitionIntervalEnabled = false;
            IsEndBehaviourEnabled = false;
            IsEnableLoopingEnabled = false;
            IsLoopInfinitelyEnabled = false;
            IsNumberOfLoopsEnabled = false;
            IsEnableTransitionsEnabled = false;
            IsTransitionTypeEnabled = false;
            IsDurationEnabled = false;
        }

        private void ResetAudioFiles()
        {
            AudioFiles.Clear();
        }

        [RelayCommand] public void ResetAudioSettings()
        {
            SetInitialAudioSettings();
            ResetAudioFiles();
        }

        private void ResetAudioSettingsGeneral()
        {
            Volume = null;
            InitialDelay = null;
        }

        private void ResetAudioSettingsPlaylist()
        {
            PlaylistType = null;
            PlaylistMode = null;
            EnableRepetitionInterval = false;
            RepetitionInterval = null;
            EndBehaviour = null;
        }

        private void ResetAudioSettingsPlaylistLooping()
        {
            EnableLooping = false;
            LoopInfinitely = false;
            NumberOfLoops = null;
        }

        private void ResetAudioSettingsTransitions()
        {
            EnableTransitions = false;
            Transition = null;
            Duration = null;
        }

        public void ResetAudioSettingsView()
        {
            IsAudioSettingsVisible = false;
        }

        public void ResetShowSettingsFromAudioProjectViewer()
        {
            ShowSettingsFromAudioProjectViewer = false;
        }

        public void Close() {}
    }
}
