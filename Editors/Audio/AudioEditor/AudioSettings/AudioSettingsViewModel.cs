using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Shared.Core.Events;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

// TODO: Some bug where the audio settings aren't updating right after multiple sounds are set after single previously being set
namespace Editors.Audio.AudioEditor.AudioSettings
{
    public partial class AudioSettingsViewModel : ObservableObject
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        [ObservableProperty] public ObservableCollection<AudioFile> _audioFiles = [];

        // Playlist Type
        [ObservableProperty] private bool _isPlaylistTypeSectionVisible = false;
        [ObservableProperty] private PlaylistType _playlistType;
        [ObservableProperty] private ObservableCollection<PlaylistType> _playlistTypes = new(Enum.GetValues<PlaylistType>());
        [ObservableProperty] private bool _isPlaylistTypeEnabled = false;
        [ObservableProperty] private bool _isPlaylistTypeVisible = false;
        [ObservableProperty] private bool _enableRepetitionInterval = false;
        [ObservableProperty] private bool _isEnableRepetitionIntervalEnabled = false;
        [ObservableProperty] private uint _repetitionInterval = 1;
        [ObservableProperty] private bool _isRepetitionIntervalEnabled = false;
        [ObservableProperty] private bool _isRepetitionIntervalVisible = false;
        [ObservableProperty] private EndBehaviour _endBehaviour = EndBehaviour.Restart;
        [ObservableProperty] private ObservableCollection<EndBehaviour> _endBehaviours = new(Enum.GetValues<EndBehaviour>());
        [ObservableProperty] private bool _isEndBehaviourEnabled = false;
        [ObservableProperty] private bool _isEndBehaviourVisible = false;

        // Playlist Mode
        [ObservableProperty] private bool _isPlaylistModeSectionVisible = false;
        [ObservableProperty] private PlaylistMode _playlistMode;
        [ObservableProperty] private ObservableCollection<PlaylistMode> _playlistModes = new(Enum.GetValues<PlaylistMode>());
        [ObservableProperty] private bool _isPlaylistModeEnabled = false;
        [ObservableProperty] private bool _isPlaylistModeVisible = false;
        [ObservableProperty] private bool _alwaysResetPlaylist;
        [ObservableProperty] private bool _isAlwaysResetPlaylistEnabled;
        [ObservableProperty] private bool _isAlwaysResetPlaylistVisible = false;
        [ObservableProperty] private LoopingType _loopingType;
        [ObservableProperty] private ObservableCollection<LoopingType> _loopingTypes = new(Enum.GetValues<LoopingType>());
        [ObservableProperty] private bool _isLoopingTypeEnabled = false;
        [ObservableProperty] private bool _isLoopingTypeVisible = false;
        [ObservableProperty] private uint _numberOfLoops = 1;
        [ObservableProperty] private bool _isNumberOfLoopsEnabled = false;
        [ObservableProperty] private bool _isNumberOfLoopsVisible = false;
        [ObservableProperty] private TransitionType _transitionType;
        [ObservableProperty] private ObservableCollection<TransitionType> _transitionTypes = new(Enum.GetValues<TransitionType>());
        [ObservableProperty] private bool _isTransitionTypeEnabled = false;
        [ObservableProperty] private bool _isTransitionTypeVisible = false;
        [ObservableProperty] private decimal _transitionDuration = 1;
        [ObservableProperty] private bool _isTransitionDurationEnabled = false;
        [ObservableProperty] private bool _isTransitionDurationVisible = false;
        [ObservableProperty] private bool _isAudioSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        public AudioSettingsViewModel(IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;

            SetInitialAudioSettings();

            _eventHub.Register<ShowSettingsFromAudioProjectViewerCheckEvent>(this, CheckShowSettingsFromAudioProjectViewer);
            _eventHub.Register<AudioFilesSetEvent>(this, OnAudioFilesSet);
            _eventHub.Register<ViewerRowSelectionChangedEvent>(this, OnViewerRowSelectionChanged);

            _eventHub.Register<ExplorerNodeSelectedEvent>(this, OnSelectedExplorerNodeChanged);
            _eventHub.Register<EditViewerRowEvent>(this, OnViewerRowEdited);
        }

        public void CheckShowSettingsFromAudioProjectViewer(ShowSettingsFromAudioProjectViewerCheckEvent resetAudioSettingsEvent)
        {
            if (ShowSettingsFromAudioProjectViewer)
                ResetShowSettingsFromAudioProjectViewer();
        }

        public void OnAudioFilesSet(AudioFilesSetEvent audioFilesSetEvent)
        {
            AudioFiles.Clear();
            AudioFiles = audioFilesSetEvent.AudioFiles;

            SetAudioSettingsEnablementAndVisibility();
            ResetShowSettingsFromAudioProjectViewer();
            StoreAudioSettings();
        }

        public void OnViewerRowSelectionChanged(ViewerRowSelectionChangedEvent viewerRowSelectionChangedEvent)
        {
            if (ShowSettingsFromAudioProjectViewer)
            {
                ShowSettingsFromAudioProjectViewerItem();
                DisableAllAudioSettings();
            }
        }

        partial void OnAudioFilesChanged(ObservableCollection<AudioFile> value)
        {
            _audioEditorService.AudioFiles = AudioFiles;
        }

        public void SetAudioFilesViaDrop(ObservableCollection<AudioFile> audioFiles)
        {
            _audioEditorService.AudioFiles = audioFiles;
            _eventHub.Publish(new AudioFilesSetEvent(audioFiles));
        }

        public void OnViewerRowEdited(EditViewerRowEvent itemEditedEvent)
        {
            ShowSettingsFromAudioProjectViewerItem();
            SetAudioSettingsEnablementAndVisibility();
        }

        public void OnSelectedExplorerNodeChanged(ExplorerNodeSelectedEvent e)
        {
            ResetAudioSettingsView();
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnShowSettingsFromAudioProjectViewerChanged(bool oldValue, bool newValue)
        {
            if (ShowSettingsFromAudioProjectViewer == false)
            {
                SetInitialAudioSettings();
                SetAudioSettingsEnablementAndVisibility();
            }
        }

        partial void OnPlaylistTypeChanged(PlaylistType oldValue, PlaylistType newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
            StoreAudioSettings();
        }

        partial void OnPlaylistModeChanged(PlaylistMode oldValue, PlaylistMode newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
            StoreAudioSettings();
        }

        partial void OnLoopingTypeChanged(LoopingType oldValue, LoopingType newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
            StoreAudioSettings();
        }

        partial void OnNumberOfLoopsChanged(uint oldValue, uint newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
            StoreAudioSettings();
        }

        partial void OnTransitionTypeChanged(TransitionType oldValue, TransitionType newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
            StoreAudioSettings();
        }

        partial void OnEnableRepetitionIntervalChanged(bool oldValue, bool newValue)
        {
            StoreAudioSettings();
        }

        partial void OnRepetitionIntervalChanged(uint oldValue, uint newValue)
        {
            StoreAudioSettings();
        }

        partial void OnEndBehaviourChanged(EndBehaviour oldValue, EndBehaviour newValue)
        {
            StoreAudioSettings();
        }

        partial void OnAlwaysResetPlaylistChanged(bool oldValue, bool newValue)
        {
            StoreAudioSettings();
        }

        partial void OnTransitionDurationChanged(decimal oldValue, decimal newValue)
        {
            StoreAudioSettings();
        }

        private SoundSettings BuildSoundSettings()
        {
            return new SoundSettings
            {
                LoopingType = LoopingType,
                NumberOfLoops = LoopingType == LoopingType.FiniteLooping ? NumberOfLoops : 1
            };
        }

        private RanSeqContainerSettings BuildRanSeqContainerSettings()
        {
            return new RanSeqContainerSettings
            {
                PlaylistType = PlaylistType,
                EnableRepetitionInterval = EnableRepetitionInterval,
                RepetitionInterval = RepetitionInterval,
                EndBehaviour = EndBehaviour,
                AlwaysResetPlaylist = AlwaysResetPlaylist,
                PlaylistMode = PlaylistMode,
                LoopingType = LoopingType,
                NumberOfLoops = LoopingType == LoopingType.FiniteLooping ? NumberOfLoops : 1,
                TransitionType = TransitionType,
                TransitionDuration = TransitionType != TransitionType.Disabled ? TransitionDuration : 1
            };
        }

        private IAudioSettings BuildAudioSettings()
        {
            if (AudioFiles.Count > 1)
                return BuildRanSeqContainerSettings();
            else
                return BuildSoundSettings();
        }

        public void StoreAudioSettings() => _audioEditorService.AudioSettings = BuildAudioSettings();

        public void ShowSettingsFromAudioProjectViewerItem()
        {
            IAudioSettings audioSettings = null;
            var audioFiles = new List<AudioFile>();

            var audioProjectItem = _audioEditorService.SelectedExplorerNode;
            if (audioProjectItem.IsActionEventSoundBank())
            {
                var selectedViewerRow = _audioEditorService.SelectedViewerRows[0];
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromActionEvent(_audioEditorService.AudioProject, selectedViewerRow);
                var actionEvent = AudioProjectHelpers.GetActionEventFromRow(_audioEditorService.AudioProject, selectedViewerRow);

                if (actionEvent.Sound != null)
                {
                    audioFiles.Add(new AudioFile()
                    {
                        FileName = actionEvent.Sound.WavFileName,
                        FilePath = actionEvent.Sound.WavFilePath,
                    });
                }
                else
                {
                    foreach (var sound in actionEvent.RandomSequenceContainer.Sounds)
                    {
                        audioFiles.Add(new AudioFile()
                        {
                            FileName = sound.WavFileName,
                            FilePath = sound.WavFilePath,
                        });
                    }
                }
            }
            else if (audioProjectItem.IsDialogueEvent())
            {
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromStatePath(_audioEditorService, _audioRepository);

                var selectedViewerRow = _audioEditorService.SelectedViewerRows[0];
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
                var statePath = AudioProjectHelpers.GetStatePathFromRow(_audioRepository, dialogueEvent, selectedViewerRow);

                if (statePath.Sound != null)
                {
                    audioFiles.Add(new AudioFile()
                    {
                        FileName = statePath.Sound.WavFileName,
                        FilePath = statePath.Sound.WavFilePath,
                    });
                }
                else
                {
                    foreach (var sound in statePath.RandomSequenceContainer.Sounds)
                    {
                        audioFiles.Add(new AudioFile()
                        {
                            FileName = sound.WavFileName,
                            FilePath = sound.WavFilePath,
                        });
                    }
                }

            }

            SetAudioSettingsFromAudioProjectItemAudioSettings(audioSettings, audioFiles);
        }

        public void SetAudioSettingsFromAudioProjectItemAudioSettings(IAudioSettings audioSettings, List<AudioFile> audioFiles)
        {
            ResetAudioFiles();
            ResetAudioSettingsPlaylistType();
            ResetAudioSettingsPlaylistMode();

            SetAudioFiles(audioFiles);

            if (audioSettings is RanSeqContainerSettings ranSeqContainerSettings)
            {
                PlaylistType = ranSeqContainerSettings.PlaylistType;

                if (ranSeqContainerSettings.PlaylistType == PlaylistType.Sequence)
                    EndBehaviour = ranSeqContainerSettings.EndBehaviour;
                else
                {
                    EnableRepetitionInterval = ranSeqContainerSettings.EnableRepetitionInterval;

                    if (EnableRepetitionInterval)
                        RepetitionInterval = ranSeqContainerSettings.RepetitionInterval;
                }

                AlwaysResetPlaylist = ranSeqContainerSettings.AlwaysResetPlaylist;

                PlaylistMode = ranSeqContainerSettings.PlaylistMode;

                LoopingType = ranSeqContainerSettings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = ranSeqContainerSettings.NumberOfLoops;

                TransitionType = ranSeqContainerSettings.TransitionType;
                if (TransitionType != TransitionType.Disabled)
                    TransitionDuration = ranSeqContainerSettings.TransitionDuration;
            }
            else if (audioSettings is SoundSettings soundSettings)
            {
                LoopingType = soundSettings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = soundSettings.NumberOfLoops;
            }

            SetAudioSettingsEnablementAndVisibility();
        }

        public void SetAudioFiles(List<AudioFile> audioFiles)
        {
            foreach (var audioFile in audioFiles)
                AudioFiles.Add(audioFile);
        }

        public void SetInitialAudioSettings()
        {
            PlaylistType = PlaylistType.Random;
            RepetitionInterval = 1;
            EndBehaviour = EndBehaviour.Restart;
            AlwaysResetPlaylist = false;

            PlaylistMode = PlaylistMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = 1;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = 1;
        }

        public void SetAudioSettingsEnablementAndVisibility()
        {
            IsAudioSettingsVisible = true;

            if (AudioFiles.Count > 1)
            {
                IsPlaylistTypeSectionVisible = true;
                IsPlaylistModeSectionVisible = true;

                IsPlaylistTypeEnabled = true;
                IsPlaylistModeEnabled = true;

                IsPlaylistTypeVisible = true;
                IsPlaylistModeVisible = true;

                if (PlaylistType == PlaylistType.Sequence)
                {
                    IsEndBehaviourEnabled = true;
                    IsEnableRepetitionIntervalEnabled = false;
                    IsRepetitionIntervalEnabled = false;
                    IsAlwaysResetPlaylistEnabled = true;

                    IsEndBehaviourVisible = true;
                    IsRepetitionIntervalVisible = false;
                    IsAlwaysResetPlaylistVisible = true;
                }
                else
                {
                    IsEnableRepetitionIntervalEnabled = true;
                    IsRepetitionIntervalEnabled = true;
                    IsEndBehaviourEnabled = false;
                    IsAlwaysResetPlaylistEnabled = false;

                    IsEndBehaviourVisible = false;
                    IsRepetitionIntervalVisible = true;
                    IsAlwaysResetPlaylistVisible = false;
                }

                if (PlaylistMode == PlaylistMode.Continuous)
                {
                    IsLoopingTypeVisible = true;
                    IsTransitionTypeVisible = true;

                    IsLoopingTypeEnabled = true;
                    if (LoopingType == LoopingType.FiniteLooping)
                    {
                        IsNumberOfLoopsEnabled = true;
                        IsNumberOfLoopsVisible = true;
                    }
                    else
                    {
                        IsNumberOfLoopsEnabled = false;
                        IsNumberOfLoopsVisible = false;
                    }

                    IsTransitionTypeEnabled = true;
                    if (TransitionType == TransitionType.Disabled)
                    {
                        IsTransitionDurationEnabled = false;
                        IsTransitionDurationVisible = false;
                    }
                    else
                    {
                        IsTransitionDurationEnabled = true;
                        IsTransitionDurationVisible = true;
                    }
                }
                else
                {
                    IsAlwaysResetPlaylistEnabled = false;
                    IsLoopingTypeEnabled = false;
                    IsNumberOfLoopsEnabled = false;
                    IsTransitionTypeEnabled = false;
                    IsTransitionDurationEnabled = false;

                    IsAlwaysResetPlaylistVisible = false;
                    IsLoopingTypeVisible = false;
                    IsNumberOfLoopsVisible = false;
                    IsTransitionTypeVisible = false;
                    IsTransitionDurationVisible = false;
                }
            }
            else
            {
                IsPlaylistTypeSectionVisible = false;

                IsPlaylistTypeEnabled = false;
                IsRepetitionIntervalEnabled = false;
                IsEndBehaviourEnabled = false;

                IsPlaylistTypeVisible = false;
                IsRepetitionIntervalVisible = false;
                IsEndBehaviourVisible = false;

                IsPlaylistModeSectionVisible = true;

                IsPlaylistModeEnabled = false;
                IsAlwaysResetPlaylistEnabled = false;
                IsNumberOfLoopsEnabled = true;
                IsTransitionTypeEnabled = false;
                IsTransitionDurationEnabled = false;

                IsPlaylistModeVisible = false;
                IsAlwaysResetPlaylistVisible = false;
                IsLoopingTypeVisible = true;
                IsNumberOfLoopsVisible = true;
                IsTransitionTypeVisible = false;
                IsTransitionDurationVisible = false;

                IsLoopingTypeEnabled = true;
                if (LoopingType == LoopingType.FiniteLooping)
                    IsNumberOfLoopsEnabled = true;
                else
                    IsNumberOfLoopsEnabled = false;
            }
        }

        [RelayCommand] public void SetRecommendedAudioSettings()
        {
            if (AudioFiles.Count > 1)
            {
                PlaylistType = PlaylistType.RandomExhaustive;
                EnableRepetitionInterval = true;
                RepetitionInterval = (uint)Math.Ceiling(AudioFiles.Count / 2.0);
                EndBehaviour = EndBehaviour.Restart;
                AlwaysResetPlaylist = true;
                PlaylistMode = PlaylistMode.Step;
                LoopingType = LoopingType.Disabled;
                NumberOfLoops = 1;
                TransitionType = TransitionType.Disabled;
                TransitionDuration = 1;
            }
        }

        public void DisableAllAudioSettings()
        {
            IsPlaylistTypeEnabled = false;
            IsEnableRepetitionIntervalEnabled = false;
            IsRepetitionIntervalEnabled = false;
            IsEndBehaviourEnabled = false;
            IsPlaylistModeEnabled = false;
            IsAlwaysResetPlaylistEnabled = false;
            IsLoopingTypeEnabled = false;
            IsNumberOfLoopsEnabled = false;
            IsTransitionTypeEnabled = false;
            IsTransitionDurationEnabled = false;
        }

        [RelayCommand] public void ResetAudioSettings()
        {
            SetInitialAudioSettings();
            ResetAudioFiles();
            SetAudioSettingsEnablementAndVisibility();
        }

        private void ResetAudioFiles()
        {
            AudioFiles.Clear();
        }

        private void ResetAudioSettingsPlaylistType()
        {
            PlaylistType = PlaylistType.Random;
            EnableRepetitionInterval = false;
            RepetitionInterval = 1;
            EndBehaviour = EndBehaviour.Restart;
            AlwaysResetPlaylist = false;
        }

        private void ResetAudioSettingsPlaylistMode()
        {
            PlaylistMode = PlaylistMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = 1;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = 1;
        }

        public void ResetAudioSettingsView()
        {
            IsAudioSettingsVisible = false;
        }

        public void ResetShowSettingsFromAudioProjectViewer()
        {
            ShowSettingsFromAudioProjectViewer = false;
        }
    }
}
