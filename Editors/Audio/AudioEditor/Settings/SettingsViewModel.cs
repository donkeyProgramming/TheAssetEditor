using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.Storage;
using Shared.Core.Events;
using static Editors.Audio.AudioEditor.Settings.Settings;

// TODO: Some bug where the audio settings aren't updating right after multiple sounds are set after single previously being set
namespace Editors.Audio.AudioEditor.Settings
{
    public partial class SettingsViewModel : ObservableObject
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
        [ObservableProperty] private bool _isSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        public SettingsViewModel(IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;

            SetInitialSettings();

            _eventHub.Register<ShowSettingsFromAudioProjectViewerCheckEvent>(this, CheckShowSettingsFromAudioProjectViewer);
            _eventHub.Register<AudioFilesSetEvent>(this, OnAudioFilesSet);
            _eventHub.Register<ViewerRowSelectionChangedEvent>(this, OnViewerRowSelectionChanged);

            _eventHub.Register<ExplorerNodeSelectedEvent>(this, OnSelectedExplorerNodeChanged);
            _eventHub.Register<EditViewerRowEvent>(this, OnViewerRowEdited);
        }

        public void CheckShowSettingsFromAudioProjectViewer(ShowSettingsFromAudioProjectViewerCheckEvent resetSettingsEvent)
        {
            if (ShowSettingsFromAudioProjectViewer)
                ResetShowSettingsFromAudioProjectViewer();
        }

        public void OnAudioFilesSet(AudioFilesSetEvent audioFilesSetEvent)
        {
            AudioFiles.Clear();
            AudioFiles = audioFilesSetEvent.AudioFiles;

            SetSettingsEnablementAndVisibility();
            ResetShowSettingsFromAudioProjectViewer();
            StoreSettings();
        }

        public void OnViewerRowSelectionChanged(ViewerRowSelectionChangedEvent viewerRowSelectionChangedEvent)
        {
            if (ShowSettingsFromAudioProjectViewer)
            {
                ShowSettingsFromAudioProjectViewerItem();
                DisableAllSettings();
            }
        }

        partial void OnAudioFilesChanged(ObservableCollection<AudioFile> value)
        {
            _audioEditorService.AudioFiles = AudioFiles.ToList();
        }

        public void SetAudioFilesViaDrop(ObservableCollection<AudioFile> audioFiles)
        {
            _audioEditorService.AudioFiles = audioFiles.ToList();
            _eventHub.Publish(new AudioFilesSetEvent(audioFiles));
        }

        public void OnViewerRowEdited(EditViewerRowEvent itemEditedEvent)
        {
            ShowSettingsFromAudioProjectViewerItem();
            SetSettingsEnablementAndVisibility();
        }

        public void OnSelectedExplorerNodeChanged(ExplorerNodeSelectedEvent e)
        {
            ResetSettingsView();
            SetSettingsEnablementAndVisibility();
        }

        partial void OnShowSettingsFromAudioProjectViewerChanged(bool oldValue, bool newValue)
        {
            if (ShowSettingsFromAudioProjectViewer == false)
            {
                SetInitialSettings();
                SetSettingsEnablementAndVisibility();
            }
        }

        partial void OnPlaylistTypeChanged(PlaylistType oldValue, PlaylistType newValue)
        {
            SetSettingsEnablementAndVisibility();
            StoreSettings();
        }

        partial void OnPlaylistModeChanged(PlaylistMode oldValue, PlaylistMode newValue)
        {
            SetSettingsEnablementAndVisibility();
            StoreSettings();
        }

        partial void OnLoopingTypeChanged(LoopingType oldValue, LoopingType newValue)
        {
            SetSettingsEnablementAndVisibility();
            StoreSettings();
        }

        partial void OnNumberOfLoopsChanged(uint oldValue, uint newValue)
        {
            SetSettingsEnablementAndVisibility();
            StoreSettings();
        }

        partial void OnTransitionTypeChanged(TransitionType oldValue, TransitionType newValue)
        {
            SetSettingsEnablementAndVisibility();
            StoreSettings();
        }

        partial void OnEnableRepetitionIntervalChanged(bool oldValue, bool newValue)
        {
            StoreSettings();
        }

        partial void OnRepetitionIntervalChanged(uint oldValue, uint newValue)
        {
            StoreSettings();
        }

        partial void OnEndBehaviourChanged(EndBehaviour oldValue, EndBehaviour newValue)
        {
            StoreSettings();
        }

        partial void OnAlwaysResetPlaylistChanged(bool oldValue, bool newValue)
        {
            StoreSettings();
        }

        partial void OnTransitionDurationChanged(decimal oldValue, decimal newValue)
        {
            StoreSettings();
        }

        public void StoreSettings()
        {
            _audioEditorService.AudioSettings = new AudioSettings
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

        public void ShowSettingsFromAudioProjectViewerItem()
        {
            AudioSettings settings = null;
            var audioFiles = new List<AudioFile>();

            var selectedExplorerNode = _audioEditorService.SelectedExplorerNode;
            if (selectedExplorerNode.IsActionEventSoundBank())
            {
                var selectedViewerRow = _audioEditorService.SelectedViewerRows[0];
                var actionEventName = DataGridHelpers.GetActionEventNameFromRow(selectedViewerRow);
                var actionEvent = _audioEditorService.AudioProject.GetActionEvent(actionEventName);
                settings = actionEvent.GetAudioSettings();

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
            else if (selectedExplorerNode.IsDialogueEvent())
            {
                var selectedViewerRow = _audioEditorService.SelectedViewerRows[0];
                var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(selectedExplorerNode.Name);
                var statePath = dialogueEvent.GetStatePath(_audioRepository, selectedViewerRow);
                settings = statePath.GetAudioSettings();

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

            SetSettingsFromAudioProjectItemSettings(settings, audioFiles);
        }

        public void SetSettingsFromAudioProjectItemSettings(AudioSettings settings, List<AudioFile> audioFiles)
        {
            ResetAudioFiles();
            ResetSettingsPlaylistType();
            ResetSettingsPlaylistMode();

            SetAudioFiles(audioFiles);

            if (audioFiles.Count > 1)
            {
                PlaylistType = settings.PlaylistType;

                if (settings.PlaylistType == PlaylistType.Sequence)
                    EndBehaviour = settings.EndBehaviour;
                else
                {
                    EnableRepetitionInterval = settings.EnableRepetitionInterval;

                    if (EnableRepetitionInterval)
                        RepetitionInterval = settings.RepetitionInterval;
                }

                AlwaysResetPlaylist = settings.AlwaysResetPlaylist;

                PlaylistMode = settings.PlaylistMode;

                LoopingType = settings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = settings.NumberOfLoops;

                TransitionType = settings.TransitionType;
                if (TransitionType != TransitionType.Disabled)
                    TransitionDuration = settings.TransitionDuration;
            }
            else
            {
                LoopingType = settings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = settings.NumberOfLoops;
            }

            SetSettingsEnablementAndVisibility();
        }

        public void SetAudioFiles(List<AudioFile> audioFiles)
        {
            foreach (var audioFile in audioFiles)
                AudioFiles.Add(audioFile);
        }

        public void SetInitialSettings()
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

        public void SetSettingsEnablementAndVisibility()
        {
            IsSettingsVisible = true;

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

        [RelayCommand] public void SetRecommendedSettings()
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

        public void DisableAllSettings()
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

        [RelayCommand] public void ResetSettings()
        {
            SetInitialSettings();
            ResetAudioFiles();
            SetSettingsEnablementAndVisibility();
        }

        private void ResetAudioFiles()
        {
            AudioFiles.Clear();
        }

        private void ResetSettingsPlaylistType()
        {
            PlaylistType = PlaylistType.Random;
            EnableRepetitionInterval = false;
            RepetitionInterval = 1;
            EndBehaviour = EndBehaviour.Restart;
            AlwaysResetPlaylist = false;
        }

        private void ResetSettingsPlaylistMode()
        {
            PlaylistMode = PlaylistMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = 1;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = 1;
        }

        public void ResetSettingsView()
        {
            IsSettingsVisible = false;
        }

        public void ResetShowSettingsFromAudioProjectViewer()
        {
            ShowSettingsFromAudioProjectViewer = false;
        }
    }
}
