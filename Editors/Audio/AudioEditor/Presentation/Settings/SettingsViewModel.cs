using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise;
using Shared.Core.Events;
using HircSettings = Editors.Audio.Shared.AudioProject.Models.HircSettings;

namespace Editors.Audio.AudioEditor.Presentation.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IEventHub _eventHub;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioRepository _audioRepository;

        [ObservableProperty] public ObservableCollection<AudioFile> _audioFiles = [];

        [ObservableProperty] private bool _isSettingsVisible = false;
        [ObservableProperty] private bool _showSettingsFromAudioProjectViewer = false;

        [ObservableProperty] private ContainerType _containerType;
        [ObservableProperty] private ObservableCollection<ContainerType> _containerTypes = new(Enum.GetValues<ContainerType>());
        [ObservableProperty] private bool _isContainerTypeEnabled = false;
        [ObservableProperty] private bool _isContainerTypeVisible = false;
        [ObservableProperty] private RandomType _randomType;
        [ObservableProperty] private ObservableCollection<RandomType> _randomTypes = new(Enum.GetValues<RandomType>());
        [ObservableProperty] private bool _isRandomTypeEnabled = false;
        [ObservableProperty] private bool _isRandomTypeVisible = false;
        [ObservableProperty] private bool _enableRepetitionInterval = false;
        [ObservableProperty] private bool _isEnableRepetitionIntervalEnabled = false;
        [ObservableProperty] private uint _repetitionInterval = 1;
        [ObservableProperty] private bool _isRepetitionIntervalEnabled = false;
        [ObservableProperty] private bool _isRepetitionIntervalVisible = false;
        [ObservableProperty] private PlaylistEndBehaviour _playlistEndBehaviour = PlaylistEndBehaviour.Restart;
        [ObservableProperty] private ObservableCollection<PlaylistEndBehaviour> _playlistEndBehaviours = new(Enum.GetValues<PlaylistEndBehaviour>());
        [ObservableProperty] private bool _isPlaylistEndBehaviourEnabled = false;
        [ObservableProperty] private bool _isPlaylistEndBehaviourVisible = false;
        [ObservableProperty] private PlayMode _playMode;
        [ObservableProperty] private ObservableCollection<PlayMode> _playModes = new(Enum.GetValues<PlayMode>());
        [ObservableProperty] private bool _IsPlayModeEnabled = false;
        [ObservableProperty] private bool _IsPlayModeVisible = false;
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

        public SettingsViewModel(IEventHub eventHub, IUiCommandFactory uiCommandFactory, IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository)
        {
            _eventHub = eventHub;
            _uiCommandFactory = uiCommandFactory;
            _audioEditorStateService = audioEditorStateService;
            _audioRepository = audioRepository;

            SetInitialSettings();

            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _eventHub.Register<EditorDataGridTextboxTextChangedEvent>(this, OnEditorDataGridTextboxTextChanged);
            _eventHub.Register<AudioFilesChangedEvent>(this, OnAudioFilesChanged);
            _eventHub.Register<ViewerTableRowSelectionChangedEvent>(this, OnViewerTableRowSelectionChanged);
            _eventHub.Register<ViewerTableRowEditedEvent>(this, OnViewerRowEdited);
        }

        private void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
        {
            IsSettingsVisible = false;
            SetSettingsUsability();
        }

        private void OnEditorDataGridTextboxTextChanged(EditorDataGridTextboxTextChangedEvent e) => CheckShowSettingsFromAudioProjectViewer();

        private void CheckShowSettingsFromAudioProjectViewer()
        {
            if (ShowSettingsFromAudioProjectViewer)
                ShowSettingsFromAudioProjectViewer = false;
        }

        private void OnAudioFilesChanged(AudioFilesChangedEvent e)
        {
            if (e.AddToExistingAudioFiles)
            {
                foreach (var audioFile in e.AudioFiles)
                    AudioFiles.Add(audioFile);
            }
            else
            {
                AudioFiles.Clear();
                AudioFiles = new ObservableCollection<AudioFile>(e.AudioFiles);
            }

            _audioEditorStateService.StoreAudioFiles(AudioFiles.ToList());

            if (e.IsSetFromViewerItem == false)
                ShowSettingsFromAudioProjectViewer = false;

            SetSettingsUsabilityAndStore();
        }

        partial void OnAudioFilesChanged(ObservableCollection<AudioFile> value) => _audioEditorStateService.StoreAudioFiles(AudioFiles.ToList());

        private void OnViewerTableRowSelectionChanged(ViewerTableRowSelectionChangedEvent e)
        {
            if (ShowSettingsFromAudioProjectViewer)
            {
                SetSettingsFromViewerItem(false);
                DisableAllSettings();
            }
        }

        private void OnViewerRowEdited(ViewerTableRowEditedEvent e)
        {
            ShowSettingsFromAudioProjectViewer = false;
            SetSettingsFromViewerItem(true);
        }

        public void SetAudioFilesViaDrop(IEnumerable<AudioFilesTreeNode> audioFilesTreeNodes)
        {
            var usedSourceIds = new HashSet<uint>();
            var audioProject = _audioEditorStateService.AudioProject;

            var audioProjectSourceIds = audioProject.GetAudioFileIds();
            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);

            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);

            var audioFiles = new List<AudioFile>();
            foreach (var node in audioFilesTreeNodes)
            {
                var audioFile = audioProject.GetAudioFile(node.FilePath);
                if (audioFile == null)
                {
                    var audioFileIds = IdGenerator.GenerateIds(usedSourceIds);
                    audioFile = AudioFile.Create(audioFileIds.Guid, audioFileIds.Id, node.FileName, node.FilePath);
                }
                audioFiles.Add(audioFile);
            }

            _audioEditorStateService.StoreAudioFiles(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, false, false, false));
        }

        public void RemoveAudioFiles(List<AudioFile> audioFilesToRemove)
        {
            if (audioFilesToRemove == null || audioFilesToRemove.Count == 0)
                return;

            var audioFiles = AudioFiles.ToList();
            foreach (var audioFileToRemove in audioFilesToRemove.ToList())
                audioFiles.Remove(audioFileToRemove);

            _audioEditorStateService.StoreAudioFiles(audioFiles);

            var newAudioFiles = new List<AudioFile>(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(newAudioFiles, false, false, false));
        }

        partial void OnShowSettingsFromAudioProjectViewerChanged(bool value)
        {
            if (ShowSettingsFromAudioProjectViewer == false)
            {
                SetInitialSettings();
                SetSettingsUsability();
            }
        }

        partial void OnContainerTypeChanged(ContainerType value) => SetSettingsUsabilityAndStore();

        partial void OnPlayModeChanged(PlayMode value) => SetSettingsUsabilityAndStore();

        partial void OnLoopingTypeChanged(LoopingType value) => SetSettingsUsabilityAndStore();

        partial void OnNumberOfLoopsChanged(uint value) => SetSettingsUsabilityAndStore();

        partial void OnTransitionTypeChanged(TransitionType value) => SetSettingsUsabilityAndStore();

        partial void OnEnableRepetitionIntervalChanged(bool value) => StoreSettings();

        partial void OnRepetitionIntervalChanged(uint value) => StoreSettings();

        partial void OnPlaylistEndBehaviourChanged(PlaylistEndBehaviour value) => StoreSettings();

        partial void OnAlwaysResetPlaylistChanged(bool value) => StoreSettings();

        partial void OnTransitionDurationChanged(decimal value) => StoreSettings();

        private void SetSettingsUsabilityAndStore()
        {
            SetSettingsUsability();
            StoreSettings();
        }

        private void SetSettingsUsability()
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (!selectedAudioProjectExplorerNode.IsDialogueEvent() && !selectedAudioProjectExplorerNode.IsActionEvent())
                return;

            IsSettingsVisible = true;

            if (AudioFiles.Count > 1)
            {
                IsContainerTypeVisible = true;
                IsContainerTypeEnabled = true;

                IsPlayModeVisible = true;
                IsPlayModeEnabled = true;

                if (ContainerType == ContainerType.Sequence)
                {
                    IsRandomTypeVisible = false;
                    IsRandomTypeEnabled = false;

                    IsPlaylistEndBehaviourVisible = true;
                    IsPlaylistEndBehaviourEnabled = true;

                    IsRepetitionIntervalVisible = false;
                    IsRepetitionIntervalEnabled = false;
                    IsEnableRepetitionIntervalEnabled = false;

                    IsAlwaysResetPlaylistEnabled = true;
                    IsAlwaysResetPlaylistVisible = true;
                }
                else
                {
                    IsRandomTypeVisible = true;
                    IsRandomTypeEnabled = true;

                    IsPlaylistEndBehaviourVisible = false;
                    IsPlaylistEndBehaviourEnabled = false;

                    IsRepetitionIntervalVisible = true;
                    IsRepetitionIntervalEnabled = true;
                    IsEnableRepetitionIntervalEnabled = true;

                    IsAlwaysResetPlaylistVisible = false;
                    IsAlwaysResetPlaylistEnabled = false;
                }

                if (PlayMode == PlayMode.Continuous)
                {
                    IsLoopingTypeVisible = true;
                    IsLoopingTypeEnabled = true;
                    if (LoopingType == LoopingType.FiniteLooping)
                    {
                        IsNumberOfLoopsVisible = true;
                        IsNumberOfLoopsEnabled = true;
                    }
                    else
                    {
                        IsNumberOfLoopsVisible = false;
                        IsNumberOfLoopsEnabled = false;
                    }

                    IsTransitionTypeVisible = true;
                    IsTransitionTypeEnabled = true;
                    if (TransitionType == TransitionType.Disabled)
                    {
                        IsTransitionDurationVisible = false;
                        IsTransitionDurationEnabled = false;
                    }
                    else
                    {
                        IsTransitionDurationVisible = true;
                        IsTransitionDurationEnabled = true;
                    }
                }
                else
                {
                    IsAlwaysResetPlaylistVisible = false;
                    IsAlwaysResetPlaylistEnabled = false;

                    IsLoopingTypeVisible = false;
                    IsLoopingTypeEnabled = false;

                    IsNumberOfLoopsVisible = false;
                    IsNumberOfLoopsEnabled = false;

                    IsTransitionTypeVisible = false;
                    IsTransitionTypeEnabled = false;

                    IsTransitionDurationEnabled = false;
                    IsTransitionDurationVisible = false;
                }
            }
            else
            {
                IsContainerTypeVisible = false;
                IsContainerTypeEnabled = false;

                IsRandomTypeVisible = false;
                IsRandomTypeEnabled = false;

                IsRepetitionIntervalVisible = false;
                IsRepetitionIntervalEnabled = false;

                IsPlaylistEndBehaviourVisible = false;
                IsPlaylistEndBehaviourEnabled = false;

                IsPlayModeVisible = false;
                IsPlayModeEnabled = false;

                IsAlwaysResetPlaylistVisible = false;
                IsAlwaysResetPlaylistEnabled = false;

                IsNumberOfLoopsVisible = true;
                IsNumberOfLoopsEnabled = true;

                IsTransitionTypeVisible = false;
                IsTransitionTypeEnabled = false;

                IsTransitionDurationVisible = false;
                IsTransitionDurationEnabled = false;

                IsLoopingTypeVisible = true;
                IsLoopingTypeEnabled = true;
                if (LoopingType == LoopingType.FiniteLooping)
                    IsNumberOfLoopsEnabled = true;
                else
                    IsNumberOfLoopsEnabled = false;
            }
        }

        private void StoreSettings()
        {
            var hircSettings = new HircSettings
            {
                ContainerType = ContainerType,
                RandomType = RandomType,
                EnableRepetitionInterval = EnableRepetitionInterval,
                RepetitionInterval = RepetitionInterval,
                PlaylistEndBehaviour = PlaylistEndBehaviour,
                AlwaysResetPlaylist = AlwaysResetPlaylist,
                PlayMode = PlayMode,
                LoopingType = LoopingType,
                NumberOfLoops = LoopingType == LoopingType.FiniteLooping ? NumberOfLoops : 1,
                TransitionType = TransitionType,
                TransitionDuration = TransitionType != TransitionType.Disabled ? TransitionDuration : 1
            };
            _audioEditorStateService.StoreHircSettings(hircSettings);
        }

        private void SetSettingsFromViewerItem(bool isRowEdited)
        {
            if (_audioEditorStateService.SelectedViewerRows.Count == 0)
                return;

            var audioProject = _audioEditorStateService.AudioProject;
            HircSettings hircSettings = null;
            var audioFiles = new List<AudioFile>();

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsActionEvent())
            {
                var selectedViewerRow = _audioEditorStateService.SelectedViewerRows[0];
                var actionEventName = TableHelpers.GetActionEventNameFromRow(selectedViewerRow);
                var actionEvent = _audioEditorStateService.AudioProject.GetActionEvent(actionEventName);
                var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(selectedAudioProjectExplorerNode.Parent.Parent.Name);

                var playActions = actionEvent.GetPlayActions();
                if (playActions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported");

                foreach (var playAction in playActions)
                {
                    if (playAction.TargetHircTypeIsSound())
                    {
                        var sound = soundBank.GetSound(playAction.TargetHircId);
                        hircSettings = sound.HircSettings;

                        var audioFile = audioProject.GetAudioFile(sound.SourceId);
                        audioFiles.Add(audioFile);
                    }
                    else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                    {
                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(playAction.TargetHircId);
                        hircSettings = randomSequenceContainer.HircSettings;

                        var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                        var orderedSounds = sounds
                            .OrderBy(sound => sound.PlaylistOrder)
                            .ToList();
                        foreach (var orderedSound in orderedSounds)
                        {
                            var audioFile = audioProject.GetAudioFile(orderedSound.SourceId);
                            audioFiles.Add(audioFile);
                        }
                    }
                }
                
            }
            else if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                var selectedViewerRow = _audioEditorStateService.SelectedViewerRows[0];
                var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(selectedAudioProjectExplorerNode.Name);
                var statePathName = TableHelpers.GetStatePathNameFromRow(selectedViewerRow, _audioRepository, selectedAudioProjectExplorerNode.Name);
                var statePath = dialogueEvent.GetStatePath(statePathName);
                var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(selectedAudioProjectExplorerNode.Parent.Parent.Name);

                if (statePath.TargetHircTypeIsSound())
                {
                    var sound = soundBank.GetSound(statePath.TargetHircId);
                    hircSettings = sound.HircSettings;

                    var audioFile = audioProject.GetAudioFile(sound.SourceId);
                    audioFiles.Add(audioFile);
                }
                else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                    hircSettings = randomSequenceContainer.HircSettings;

                    var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                    var orderedSounds = sounds
                        .OrderBy(sound => sound.PlaylistOrder)
                        .ToList();
                    foreach (var orderedSound in orderedSounds)
                    {
                        var audioFile = audioProject.GetAudioFile(orderedSound.SourceId);
                        audioFiles.Add(audioFile);
                    }
                }

            }
            else if (selectedAudioProjectExplorerNode.IsStateGroup())
                return;

            SetSettingsFromViewerItem(hircSettings, audioFiles, isRowEdited);
        }

        private void SetSettingsFromViewerItem(HircSettings hircSettings, List<AudioFile> audioFiles, bool isRowEdited)
        {
            ResetAudioFiles(true);

            ContainerType = ContainerType.Random;
            RandomType = RandomType.Standard;
            PlaylistEndBehaviour = PlaylistEndBehaviour.Restart;
            PlayMode = PlayMode.Step;
            EnableRepetitionInterval = false;
            RepetitionInterval = 1;
            AlwaysResetPlaylist = false;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = 1;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = 1;

            SetAudioFilesFromViewerItem(audioFiles, isRowEdited);

            if (audioFiles.Count > 1)
            {
                ContainerType = hircSettings.ContainerType;

                if (hircSettings.ContainerType == ContainerType.Sequence)
                    PlaylistEndBehaviour = hircSettings.PlaylistEndBehaviour;
                else
                {
                    RandomType = hircSettings.RandomType;
                    EnableRepetitionInterval = hircSettings.EnableRepetitionInterval;

                    if (EnableRepetitionInterval)
                        RepetitionInterval = hircSettings.RepetitionInterval;
                }

                AlwaysResetPlaylist = hircSettings.AlwaysResetPlaylist;

                PlayMode = hircSettings.PlayMode;

                LoopingType = hircSettings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = hircSettings.NumberOfLoops;

                TransitionType = hircSettings.TransitionType;
                if (TransitionType != TransitionType.Disabled)
                    TransitionDuration = hircSettings.TransitionDuration;
            }
            else
            {
                LoopingType = hircSettings.LoopingType;
                if (LoopingType == LoopingType.FiniteLooping)
                    NumberOfLoops = hircSettings.NumberOfLoops;
            }

            SetSettingsUsability();
        }

        private void SetAudioFilesFromViewerItem(List<AudioFile> audioFiles, bool isRowEdited)
        {
            AudioFiles = new ObservableCollection<AudioFile>(audioFiles);
            _audioEditorStateService.StoreAudioFiles(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, false, true, isRowEdited));
        }

        [RelayCommand] public void PlayWav(AudioFile audioFile)
        {
            _uiCommandFactory.Create<PlayAudioFileCommand>().Execute(audioFile.WavPackFileName, audioFile.WavPackFilePath);
        }

        private void SetInitialSettings()
        {
            ContainerType = ContainerType.Random;
            RandomType = RandomType.Standard;
            RepetitionInterval = 1;
            PlaylistEndBehaviour = PlaylistEndBehaviour.Restart;
            AlwaysResetPlaylist = false;
            PlayMode = PlayMode.Step;
            LoopingType = LoopingType.Disabled;
            NumberOfLoops = 1;
            TransitionType = TransitionType.Disabled;
            TransitionDuration = 1;
        }

        [RelayCommand] public void SetRecommendedSettings()
        {
            if (AudioFiles.Count > 1)
            {
                ContainerType = ContainerType.Random;
                RandomType = RandomType.Shuffle;
                EnableRepetitionInterval = true;
                RepetitionInterval = (uint)Math.Ceiling(AudioFiles.Count / 2.0);
                PlaylistEndBehaviour = PlaylistEndBehaviour.Restart;
                AlwaysResetPlaylist = true;
                PlayMode = PlayMode.Step;
                LoopingType = LoopingType.Disabled;
                NumberOfLoops = 1;
                TransitionType = TransitionType.Disabled;
                TransitionDuration = 1;
            }
        }

        private void DisableAllSettings()
        {
            IsContainerTypeEnabled = false;
            IsRandomTypeEnabled = false;
            IsEnableRepetitionIntervalEnabled = false;
            IsRepetitionIntervalEnabled = false;
            EnableRepetitionInterval = false;
            IsPlaylistEndBehaviourEnabled = false;
            IsPlayModeEnabled = false;
            IsAlwaysResetPlaylistEnabled = false;
            IsLoopingTypeEnabled = false;
            IsNumberOfLoopsEnabled = false;
            IsTransitionTypeEnabled = false;
            IsTransitionDurationEnabled = false;
        }

        [RelayCommand] public void ResetSettings()
        {
            SetInitialSettings();
            ResetAudioFiles(false);
            SetSettingsUsability();
        }

        private void ResetAudioFiles(bool resetForViewerItem)
        {
            AudioFiles.Clear();
            _audioEditorStateService.StoreAudioFiles(AudioFiles.ToList());
            _eventHub.Publish(new AudioFilesChangedEvent(AudioFiles.ToList(), false, resetForViewerItem, true));
        }
    }
}
