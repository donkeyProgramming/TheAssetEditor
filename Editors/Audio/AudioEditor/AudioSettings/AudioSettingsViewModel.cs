using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    public partial class AudioSettingsViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Settings";

        // General
        [ObservableProperty] private decimal _volume;
        [ObservableProperty] private bool _isVolumeEnabled = true;
        [ObservableProperty] private decimal _initialDelay = 0;
        [ObservableProperty] private bool _isInitialDelayEnabled = true;

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

        [ObservableProperty] private bool _isAudioSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        public ObservableCollection<AudioFile> AudioFiles { get; set; } = [];

        public AudioSettingsViewModel(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;
        }

        partial void OnPlaylistTypeChanged(PlaylistType oldValue, PlaylistType newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnPlaylistModeChanged(PlaylistMode oldValue, PlaylistMode newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnEnableLoopingChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnLoopInfinitelyChanged(bool oldValue, bool newValue)
        {
            UpdateLoopingEnablement();
        }

        partial void OnNumberOfLoopsChanged(uint oldValue, uint newValue)
        {
            UpdateLoopingEnablement();
        }

        partial void OnEnableTransitionsChanged(bool oldValue, bool newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        public Data.AudioSettings BuildAudioSettings()
        {
            var audioSettings = new Data.AudioSettings
            {
                AudioFiles = AudioFiles
                    .Select(audiofile => audiofile.FilePath)
                    .ToList()
            };

            if (InitialDelay > 0)
                audioSettings.InitialDelay = InitialDelay;

            if (Volume > 0)
                audioSettings.Volume = Volume;

            if (AudioFiles.Count > 1) // This should prevent the recording of any settings stored but not in use e.g. from when the user has previously applied settings with multiple sounds set
            {
                audioSettings.PlaylistType = PlaylistType;
                audioSettings.PlaylistMode = PlaylistMode;

                if (PlaylistType == PlaylistType.Sequence)
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

        public void SetAudioSettingsFromAudioProjectItem(Data.AudioSettings audioSettings)
        {
            AudioFiles.Clear();

            foreach (var audioFile in audioSettings.AudioFiles)
            {
                AudioFiles.Add(new AudioFile
                {
                    FileName = Path.GetFileName(audioFile),
                    FilePath = audioFile
                });
            }

            if (audioSettings.InitialDelay > 0)
                InitialDelay = audioSettings.InitialDelay;

            if (audioSettings.Volume > 0)
                Volume = audioSettings.Volume;

            if (audioSettings.AudioFiles.Count > 1)
            {
                PlaylistType = audioSettings.PlaylistType;
                PlaylistMode = audioSettings.PlaylistMode;

                if (audioSettings.PlaylistType == PlaylistType.Sequence)
                    EndBehaviour = audioSettings.EndBehaviour;
                else
                {
                    EnableRepetitionInterval = audioSettings.EnableRepetitionInterval;
                    if (EnableRepetitionInterval)
                        RepetitionInterval = audioSettings.RepetitionInterval;
                }

                if (audioSettings.EnableLooping)
                {
                    EnableLooping = audioSettings.EnableLooping;
                    if (LoopInfinitely)
                        LoopInfinitely = audioSettings.LoopInfinitely;
                    else
                        NumberOfLoops = audioSettings.NumberOfLoops;
                }

                if (audioSettings.EnableTransitions)
                {
                    EnableTransitions = audioSettings.EnableTransitions;
                    Transition = audioSettings.Transition;
                    Duration = audioSettings.Duration;
                }
            }

            SetAudioSettingsEnablementAndVisibility();
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

                if (PlaylistType == PlaylistType.Sequence)
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

                if (PlaylistMode == PlaylistMode.Continuous)
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
                if (PlaylistMode == PlaylistMode.Continuous)
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

        [RelayCommand] public void ResetAudioSettings()
        {
            Volume = 0;
            InitialDelay = 0;

            if (AudioFiles.Count > 1)
            {
                PlaylistType = PlaylistType.Random;
                PlaylistMode = PlaylistMode.Step;
                EnableRepetitionInterval = false;
                RepetitionInterval = 1;
                EndBehaviour = EndBehaviour.Restart;

                EnableLooping = false;
                LoopInfinitely = false;
                NumberOfLoops = 0;

                EnableTransitions = false;
                Transition = TransitionType.XfadeAmp;
                Duration = 0;
            }
        }

        [RelayCommand] public void SetRecommendedAudioSettings()
        {
            Volume = 0;
            InitialDelay = 0;

            if (AudioFiles.Count > 1)
            {
                PlaylistType = PlaylistType.RandomExhaustive;
                PlaylistMode = PlaylistMode.Step;
                EnableRepetitionInterval = true;
                RepetitionInterval = (uint)Math.Ceiling(AudioFiles.Count / 2.0);
                EndBehaviour = EndBehaviour.Restart;

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

        public void ResetAudioSettingsView()
        {
            IsAudioSettingsVisible = false;
        }

        public void Close() {}
    }
}
