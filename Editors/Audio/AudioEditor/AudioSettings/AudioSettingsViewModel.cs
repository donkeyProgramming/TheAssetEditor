using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Shared.Core.Events;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    // TODO: Add visibility to all settings to hide or show them according to the type of thing.
    // TODO: Add a way for single sounds to loop.
    public partial class AudioSettingsViewModel : ObservableObject, IEditorInterface
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public string DisplayName { get; set; } = "Audio Settings";

        // Playlist Type
        [ObservableProperty] private PlaylistType _playlistType;
        [ObservableProperty] private ObservableCollection<PlaylistType> _playlistTypes = new(Enum.GetValues<PlaylistType>());
        [ObservableProperty] private bool _isPlaylistTypeEnabled = false;
        [ObservableProperty] private bool _enableRepetitionInterval = false;
        [ObservableProperty] private bool _isEnableRepetitionIntervalEnabled = false;
        [ObservableProperty] private uint? _repetitionInterval = null;
        [ObservableProperty] private bool _isRepetitionIntervalEnabled = false;
        [ObservableProperty] private EndBehaviour _endBehaviour = EndBehaviour.Restart;
        [ObservableProperty] private ObservableCollection<EndBehaviour> _endBehaviours = new(Enum.GetValues<EndBehaviour>());
        [ObservableProperty] private bool _isEndBehaviourEnabled = false;
        [ObservableProperty] private bool _alwaysResetPlaylist;
        [ObservableProperty] private bool _isAlwaysResetPlaylistEnabled;

        // Playlist Mode
        [ObservableProperty] private PlaylistMode _playlistMode;
        [ObservableProperty] private ObservableCollection<PlaylistMode> _playlistModes = new(Enum.GetValues<PlaylistMode>());
        [ObservableProperty] private bool _isPlaylistModeEnabled = false;
        [ObservableProperty] private LoopingType _loopingType;
        [ObservableProperty] private ObservableCollection<LoopingType> _loopingTypes = new(Enum.GetValues<LoopingType>());
        [ObservableProperty] private bool _isLoopingTypeEnabled = false;
        [ObservableProperty] private uint? _numberOfLoops = null;
        [ObservableProperty] private bool _isNumberOfLoopsEnabled = false;
        [ObservableProperty] private TransitionType _transitionType;
        [ObservableProperty] private ObservableCollection<TransitionType> _transitionTypes = new(Enum.GetValues<TransitionType>());
        [ObservableProperty] private bool _isTransitionTypeEnabled = false;
        [ObservableProperty] private decimal? _transitionDuration = null;
        [ObservableProperty] private bool _isTransitionDurationEnabled = false;
        [ObservableProperty] private bool _isAudioSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        public ObservableCollection<AudioFile> AudioFiles { get; set; } = [];

        public AudioSettingsViewModel(IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;

            SetInitialAudioSettings();

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _eventHub.Register<ItemEditedEvent>(this, OnItemEdited);
        }

        public void OnItemEdited(ItemEditedEvent itemEditedEvent)
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            ShowSettingsFromAudioProjectViewerItem();
            SetAudioSettingsEnablementAndVisibility();
        }

        public void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
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
        }

        partial void OnPlaylistModeChanged(PlaylistMode oldValue, PlaylistMode newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        partial void OnLoopingTypeChanged(LoopingType oldValue, LoopingType newValue)
        {
            UpdateLoopingEnablementAndValues();
        }

        partial void OnNumberOfLoopsChanged(uint? oldValue, uint? newValue)
        {
            UpdateLoopingEnablementAndValues();
        }

        partial void OnTransitionTypeChanged(TransitionType oldValue, TransitionType newValue)
        {
            SetAudioSettingsEnablementAndVisibility();
        }

        public AudioProjectData.AudioSettings BuildAudioSettings()
        {
            var audioSettings = new AudioProjectData.AudioSettings();

            if (AudioFiles.Count > 1)
            {
                audioSettings.PlaylistType = PlaylistType;

                if (PlaylistType == PlaylistType.Sequence)
                    audioSettings.EndBehaviour = EndBehaviour;
                else
                {
                    audioSettings.EnableRepetitionInterval = EnableRepetitionInterval;

                    if (EnableRepetitionInterval)
                        audioSettings.RepetitionInterval = RepetitionInterval;
                }

                audioSettings.AlwaysResetPlaylist = AlwaysResetPlaylist;

                audioSettings.PlaylistMode = PlaylistMode;
                audioSettings.LoopingType = LoopingType;

                if (LoopingType == LoopingType.FiniteLooping)
                        audioSettings.NumberOfLoops = NumberOfLoops;

                if (TransitionType != TransitionType.Disabled)
                {
                    audioSettings.TransitionType = TransitionType;
                    audioSettings.TransitionDuration = TransitionDuration;
                }
            }

            return audioSettings;
        }

        public void ShowSettingsFromAudioProjectViewerItem()
        {
            var audioSettings = new AudioProjectData.AudioSettings();
            var audioFiles = new List<AudioFile>();

            var audioProjectItem = _audioEditorService.GetSelectedExplorerNode();
            if (audioProjectItem.NodeType == NodeType.ActionEventSoundBank)
            {
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromAudioProjectViewerActionEvent(_audioEditorService);

                var selectedAudioProjectViewerDataGridRow = _audioEditorService.GetSelectedViewerRows()[0];
                var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioProjectItem.Name);
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(selectedAudioProjectViewerDataGridRow, soundBank);

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
            else if (audioProjectItem.NodeType == NodeType.DialogueEvent)
            {
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromAudioProjectViewerStatePath(_audioEditorService.AudioEditorViewModel, _audioEditorService, _audioRepository);

                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(_audioRepository, _audioEditorService.GetSelectedViewerRows()[0], dialogueEvent);

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

            SetAudioSettingsFromAudioProjectItemAudioSettings(audioSettings, audioFiles.Count);
            SetAudioFiles(audioFiles);
        }

        public void SetAudioSettingsFromAudioProjectItemAudioSettings(AudioProjectData.AudioSettings audioSettings, int audioFilesCount)
        {
            ResetAudioFiles();
            ResetAudioSettingsPlaylistType();
            ResetAudioSettingsPlaylistMode();

            if (audioFilesCount > 1)
            {
                PlaylistType = audioSettings.PlaylistType;

                if (audioSettings.PlaylistType == PlaylistType.Sequence)
                {
                    EndBehaviour = audioSettings.EndBehaviour;
                }
                else
                {
                    EnableRepetitionInterval = audioSettings.EnableRepetitionInterval;

                    if (EnableRepetitionInterval)
                        RepetitionInterval = audioSettings.RepetitionInterval;
                }

                AlwaysResetPlaylist = audioSettings.AlwaysResetPlaylist;

                PlaylistMode = audioSettings.PlaylistMode;
                LoopingType = audioSettings.LoopingType;

                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = audioSettings.NumberOfLoops;

                TransitionType = audioSettings.TransitionType;

                if (TransitionType != TransitionType.Disabled)
                    TransitionDuration = audioSettings.TransitionDuration;
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
            RepetitionInterval = null;
            EndBehaviour = EndBehaviour.Restart;
            AlwaysResetPlaylist = false;

            PlaylistMode = PlaylistMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = null;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = null;
        }

        public void SetAudioSettingsEnablementAndVisibility()
        {
            // We only want to show the settings for these nodes
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent)
                return;

            IsAudioSettingsVisible = true;

            if (AudioFiles.Count > 1)
            {
                IsPlaylistTypeEnabled = true;

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

                IsPlaylistModeEnabled = true;

                if (PlaylistMode == PlaylistMode.Continuous)
                {
                    IsLoopingTypeEnabled = true;
                    if (LoopingType == LoopingType.InfiniteLooping)
                        IsNumberOfLoopsEnabled = true;
                    else
                        IsNumberOfLoopsEnabled = false;

                    IsTransitionTypeEnabled = true;
                    if (TransitionType != TransitionType.Disabled)
                        IsTransitionDurationEnabled = true;
                    else
                        IsTransitionDurationEnabled = false;
                }
                else
                {
                    IsAlwaysResetPlaylistEnabled = false;
                    IsLoopingTypeEnabled = false;
                    IsNumberOfLoopsEnabled = false;
                    IsTransitionTypeEnabled = false;
                    IsTransitionDurationEnabled = false;
                }
            }
            else
            {
                IsPlaylistTypeEnabled = false;
                IsEndBehaviourEnabled = false;
                IsRepetitionIntervalEnabled = false;

                IsPlaylistModeEnabled = false;
                IsAlwaysResetPlaylistEnabled = false;
                IsLoopingTypeEnabled = false;
                IsNumberOfLoopsEnabled = false;
                IsTransitionTypeEnabled = false;
                IsTransitionDurationEnabled = false;
            }
        }

        public void UpdateLoopingEnablementAndValues()
        {
            if (AudioFiles.Count > 1)
            {
                if (PlaylistMode == PlaylistMode.Continuous)
                {
                    if (LoopingType == LoopingType.FiniteLooping)
                    {
                        IsNumberOfLoopsEnabled = true;

                        if (NumberOfLoops == null)
                            NumberOfLoops = 1;
                    }
                    else
                        IsNumberOfLoopsEnabled = false;
                }
            }
        }

        [RelayCommand] public void SetRecommendedAudioSettings()
        {
            if (AudioFiles.Count > 1)
            {
                PlaylistType = PlaylistType.RandomExhaustive;
                EnableRepetitionInterval = true;
                RepetitionInterval = (uint)Math.Ceiling(AudioFiles.Count / 2.0);
                EndBehaviour = AudioSettings.EndBehaviour.Restart;
                AlwaysResetPlaylist = true;

                PlaylistMode = PlaylistMode.Step;
                LoopingType = LoopingType.Disabled;
                NumberOfLoops = null;
                TransitionType = TransitionType.Disabled;
                TransitionDuration = null;
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
            RepetitionInterval = null;
            EndBehaviour = EndBehaviour.Restart;
            AlwaysResetPlaylist = false;
        }

        private void ResetAudioSettingsPlaylistMode()
        {
            PlaylistMode = PlaylistMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = null;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = null;
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
