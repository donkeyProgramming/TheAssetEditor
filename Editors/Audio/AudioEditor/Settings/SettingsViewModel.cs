using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Events;
using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.AudioEditor.Settings
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
                ShowSettingsFromViewerItem();
                DisableAllSettings();
            }
        }

        private void OnViewerRowEdited(ViewerTableRowEditedEvent e)
        {
            ShowSettingsFromAudioProjectViewer = false;
            ShowSettingsFromViewerItem();
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
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, false, false));
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
            _eventHub.Publish(new AudioFilesChangedEvent(newAudioFiles, false, false));
        }

        partial void OnShowSettingsFromAudioProjectViewerChanged(bool value)
        {
            if (ShowSettingsFromAudioProjectViewer == false)
            {
                SetInitialSettings();
                SetSettingsUsability();
            }
        }

        partial void OnPlaylistTypeChanged(PlaylistType value) => SetSettingsUsabilityAndStore();

        partial void OnPlaylistModeChanged(PlaylistMode value) => SetSettingsUsabilityAndStore();

        partial void OnLoopingTypeChanged(LoopingType value) => SetSettingsUsabilityAndStore();

        partial void OnNumberOfLoopsChanged(uint value) => SetSettingsUsabilityAndStore();

        partial void OnTransitionTypeChanged(TransitionType value) => SetSettingsUsabilityAndStore();

        partial void OnEnableRepetitionIntervalChanged(bool value) => StoreSettings();

        partial void OnRepetitionIntervalChanged(uint value) => StoreSettings();

        partial void OnEndBehaviourChanged(EndBehaviour value) => StoreSettings();

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

        private void StoreSettings()
        {
            var audioSettings = new AudioSettings
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
            _audioEditorStateService.StoreAudioSettings(audioSettings);
        }

        private void ShowSettingsFromViewerItem()
        {
            if (_audioEditorStateService.SelectedViewerRows.Count == 0)
                return;

            var audioProject = _audioEditorStateService.AudioProject;
            AudioSettings settings = null;
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
                        settings = sound.AudioSettings;

                        var audioFile = audioProject.GetAudioFile(sound.SourceId);
                        audioFiles.Add(audioFile);
                    }
                    else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                    {
                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(playAction.TargetHircId);
                        settings = randomSequenceContainer.AudioSettings;

                        var sounds = soundBank.GetSounds(randomSequenceContainer.SoundReferences);
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
                    settings = sound.AudioSettings;

                    var audioFile = audioProject.GetAudioFile(sound.SourceId);
                    audioFiles.Add(audioFile);
                }
                else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                    settings = randomSequenceContainer.AudioSettings;

                    var sounds = soundBank.GetSounds(randomSequenceContainer.SoundReferences);
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

            SetSettingsFromViewerItem(settings, audioFiles);
        }

        private void SetSettingsFromViewerItem(AudioSettings settings, List<AudioFile> audioFiles)
        {
            ResetAudioFiles(true);
            ResetSettingsPlaylistType();
            ResetSettingsPlaylistMode();

            SetAudioFilesFromViewerItem(audioFiles);

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

            SetSettingsUsability();
        }

        private void SetAudioFilesFromViewerItem(List<AudioFile> audioFiles)
        {
            AudioFiles = new ObservableCollection<AudioFile>(audioFiles);
            _audioEditorStateService.StoreAudioFiles(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, false, true));
        }

        [RelayCommand] public void PlayWav(AudioFile audioFile)
        {
            _uiCommandFactory.Create<PlayAudioFileCommand>().Execute(audioFile.WavPackFileName, audioFile.WavPackFilePath);
        }

        private void SetInitialSettings()
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

        private void DisableAllSettings()
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
            ResetAudioFiles(false);
            SetSettingsUsability();
        }

        private void ResetAudioFiles(bool resetForViewerItem)
        {
            AudioFiles.Clear();
            _audioEditorStateService.StoreAudioFiles(AudioFiles.ToList());
            _eventHub.Publish(new AudioFilesChangedEvent(AudioFiles.ToList(), false, resetForViewerItem));
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
    }
}
