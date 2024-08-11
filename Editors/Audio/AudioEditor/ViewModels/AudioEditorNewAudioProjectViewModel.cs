﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProjectConverter;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public class DialogueEventCheckBox : ObservableObject
    {
        private bool _isChecked;

        public string Content { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }
    }

    public partial class AudioEditorNewAudioProjectViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        readonly ILogger _logger = Logging.Create<AudioEditorNewAudioProjectViewModel>();
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New Audio Editor Project");

        [ObservableProperty] private string _audioProjectFileName;

        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;

        [ObservableProperty] private ObservableCollection<AudioEditorSettings.EventType> _audioProjectEventTypes = new(Enum.GetValues(typeof(AudioEditorSettings.EventType)).Cast<AudioEditorSettings.EventType>());
        [ObservableProperty] private ObservableCollection<AudioEditorSettings.EventSubtype> _audioProjectSubtypes = []; // Determined according to what Event Type is selected

        [ObservableProperty] private ObservableCollection<DialogueEventCheckBox> _dialogueEventCheckBoxes = [];
        [ObservableProperty] private bool _isAnyDialogueEventChecked;

        public static Dictionary<string, List<Dictionary<string, object>>> AudioProjectDataInstance => AudioEditorData.Instance.AudioProjectDataInstance;

        public static string AudioProjectFileNameInstance => AudioEditorData.Instance.AudioProjectFileNameInstance;


        public AudioEditorNewAudioProjectViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
        }

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();

            // Update the ComboBox for EventSubType upon EventType selection.
            UpdateAudioProjectEventSubType();
        }

        partial void OnSelectedAudioProjectEventSubtypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();

            // Update the ListBox with the appropriate Dialogue Events.
            PopulateDialogueEventsListBox();
        }

        private void HandleDialogueEventCheckBoxChanged(DialogueEventCheckBox changedItem)
        {
            IsAnyDialogueEventChecked = DialogueEventCheckBoxes.Any(checkBox => checkBox.IsChecked);
            _logger.Here().Information($"Dialogue Event: {changedItem.Content} IsChecked: {changedItem.IsChecked}");
        }

        public void UpdateAudioProjectEventSubType()
        {
            AudioProjectSubtypes.Clear();

            if (Enum.TryParse(SelectedAudioProjectEventType.ToString(), out AudioEditorSettings.EventType eventType))
            {
                if (AudioEditorSettings.EventTypeToSubtypes.TryGetValue(eventType, out var subtypes))
                {
                    foreach (var subtype in subtypes)
                        AudioProjectSubtypes.Add(subtype);
                }
            }
        }

        public void PopulateDialogueEventsListBox()
        {
            if (Enum.TryParse(SelectedAudioProjectEventType, out AudioEditorSettings.EventType eventType))
            {
                if (Enum.TryParse(SelectedAudioProjectEventSubtype, out AudioEditorSettings.EventSubtype eventSubtype))
                {
                    var dialogueEvents = AudioEditorSettings.DialogueEvents
                        .Where(de => de.Type == eventType)
                        .ToList();

                    foreach (var dialogueEvent in dialogueEvents)
                    {
                        if (dialogueEvent.Subtype.Contains(eventSubtype))
                        {
                            var item = new DialogueEventCheckBox
                            {
                                Content = AddExtraUnderScoresToString(dialogueEvent.EventName),
                                IsChecked = false
                            };

                            // Subscribe to property changes
                            item.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(DialogueEventCheckBox.IsChecked))
                                {
                                    HandleDialogueEventCheckBoxChanged(item);
                                }
                            };

                            DialogueEventCheckBoxes.Add(item);
                        }
                    }
                }
            }
        }

        [RelayCommand] public void CreateAudioProject()
        {
            if (DialogueEventCheckBoxes.All(checkBox => checkBox.IsChecked != true))
                return;

            // Remove any pre-existing data.
            AudioEditorData.Instance.AudioProjectDataInstance.Clear();
            _audioEditorViewModel.AudioEditorDataGridItems.Clear();
            _audioEditorViewModel.SelectedAudioProjectEvent = "";

            CreateAudioProjectDialogueEventsList();

            AudioEditorData.Instance.AudioProjectFileNameInstance = AudioProjectFileName;

            // Create the object for State Groups with qualifiers so that their keys in the AudioProjectConverter dictionary are unique.
            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            // Initialise AudioProjectConverter according to the Audio Project settings selected.
            _audioEditorViewModel.InitialiseAudioProjectData();

            // Add the Audio Project with empty events to the PackFile.
            AddAudioProjectToPackFile(_packFileService);

            // Load the custom States so that they can be referenced when the Event is loaded.
            //PrepareCustomStatesForComboBox(this);

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();

            AudioProjectFileName = "";
            SelectedAudioProjectEventType = "";
            SelectedAudioProjectEventSubtype = "";
            DialogueEventCheckBoxes.Clear();
            IsAnyDialogueEventChecked = false;
        }

        public void CreateAudioProjectDialogueEventsList()
        {
            _audioEditorViewModel.AudioProjectDialogueEvents.Clear();

            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (checkBox.IsChecked == true)
                {
                    var dialogueEvent = checkBox.Content.ToString();
                    _audioEditorViewModel.AudioProjectDialogueEvents.Add(RemoveExtraUnderScoresFromString(dialogueEvent));
                }
            }
        }

        [RelayCommand] public void SelectAll()
        {
            foreach (var checkBox in DialogueEventCheckBoxes)
                checkBox.IsChecked = true;
        }

        /*
        [RelayCommand] public void SelectRecommended()
        {
            // Get the list of dialogue events with the "Recommended" category
            var recommendedEvents = AudioEditorSettings.FrontendVODialogueEvents.Where(e => e.Categories.Contains("Recommended")).Select(e => e.EventName).ToHashSet();

            // Iterate through the CheckBoxes and set IsChecked for those with matching EventName
            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (recommendedEvents.Contains(checkBox.Content.ToString()))
                {
                    checkBox.IsChecked = true;
                }
            }
        }
        */

        [RelayCommand] public void SelectNone()
        {
            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
        }

        public void SetCloseAction(Action closeAction)
        {
            _closeAction = closeAction;
        }

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
