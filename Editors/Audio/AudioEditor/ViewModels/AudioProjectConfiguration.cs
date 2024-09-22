using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.AudioProjectData;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public class DialogueEventCheckBox : ObservableObject
    {
        private bool _isChecked;
        private bool _isEnabled;

        public string Content { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }

    public partial class AudioEditorViewModel
    {
        partial void OnSelectedAudioTypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();
            IsAnyAudioProjectItemChecked = false;
            IsAddToAudioProjectButtonEnabled = IsAnyAudioProjectItemChecked;

            // Update the ComboBox for EventSubType upon EventType selection.
            UpdateAudioProjectEventSubType();
        }

        partial void OnSelectedAudioSubtypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();
            IsAnyAudioProjectItemChecked = false;
            IsAddToAudioProjectButtonEnabled = IsAnyAudioProjectItemChecked;

            // Update the ListBox with the appropriate Dialogue Events.
            PopulateDialogueEventsListBox();
        }

        private void UpdateAudioProjectEventSubType()
        {
            AudioProjectSubtypes.Clear();

            if (SelectedAudioType == null)
                return;

            if (Enum.TryParse(SelectedAudioType.ToString(), out AudioType eventType) && Enum.TryParse(SelectedAudioType.ToString(), out DialogueEventType dialogueEventType))
            {
                var subtypes = GetAudioSubtypesFromAudioType(eventType);

                if (subtypes.Count != 0)
                {
                    IsAudioSubtypeEnabled = true;

                    foreach (var subtype in subtypes)
                        AudioProjectSubtypes.Add(subtype);
                }
            }

            else
            {
                IsAudioSubtypeEnabled = false;
                IsAddToAudioProjectButtonEnabled = true;
            }
        }

        private void PopulateDialogueEventsListBox()
        {
            if (Enum.TryParse(SelectedAudioType, out AudioType eventType))
            {
                if (Enum.TryParse(SelectedAudioSubtype, out AudioSubtype eventSubtype))
                {
                    var configDialogueEvents = DialogueEvents.Where(dialogueEvent => dialogueEvent.Type == eventType).ToList();
                    var audioProjectDialogueEvents = new HashSet<string>();

                    // Get a list of all the Dialogue Events in the Audio Project which are checked are used to determine the IsEnabled state of the CheckBox.
                    var dialogueEventSoundBanks = _audioProjectService.AudioProject.SoundBanks.Where(soundBank => soundBank.Type == SoundBankType.DialogueEventBnk.ToString()).ToList();

                    foreach (var soundBank in dialogueEventSoundBanks)
                    {
                        foreach (var dialogueEvent in soundBank.DialogueEvents)
                            audioProjectDialogueEvents.Add(dialogueEvent.Name);
                    }

                    // Create CheckBoxes.
                    foreach (var configDialogueEvent in configDialogueEvents)
                    {
                        if (configDialogueEvent.Subtype.Contains(eventSubtype))
                        {
                            var dialogueEventName = configDialogueEvent.EventName;
                            var isEnabled = true;

                            if (audioProjectDialogueEvents.Contains(dialogueEventName))
                                isEnabled = false;

                            var checkBox = new DialogueEventCheckBox
                            {
                                Content = AddExtraUnderscoresToString(dialogueEventName),
                                IsChecked = false,
                                IsEnabled = isEnabled
                            };

                            checkBox.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(DialogueEventCheckBox.IsChecked))
                                    HandleDialogueEventCheckBoxChanged(checkBox);
                            };

                            DialogueEventCheckBoxes.Add(checkBox);
                        }
                    }
                }
            }
        }

        private void HandleDialogueEventCheckBoxChanged(DialogueEventCheckBox changedItem)
        {
            IsAnyAudioProjectItemChecked = DialogueEventCheckBoxes.Any(checkBox => checkBox.IsChecked);

            IsAddToAudioProjectButtonEnabled = IsAnyAudioProjectItemChecked;
        }

        // NEED TO MODIFY THIS SO IT ONLY SELECTS ITEMS NOT IN THE PROJECT
        [RelayCommand] private void SelectAll()
        {
            foreach (var checkBox in DialogueEventCheckBoxes)
                checkBox.IsChecked = true;
        }

        /*
        [RelayCommand] public void SelectRecommended()
        {
            // Get the list of dialogue events with the "Recommended" category
            var recommendedEvents = FrontendVODialogueEvents.Where(e => e.Categories.Contains("Recommended")).Select(e => e.Name).ToHashSet();

            // Iterate through the CheckBoxes and set IsChecked for those with matching Name
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

        [RelayCommand] public void ResetAudioProjectConfiguration()
        {
            SelectedAudioType = null;
            SelectedAudioSubtype = null;
            IsAudioSubtypeEnabled = false;
        }

        [RelayCommand] public void AddToAudioProject()
        {
            var audioTypeEnum = (AudioType)Enum.Parse(typeof(AudioType), SelectedAudioType.ToString());
            var audioType = GetStringFromAudioType(audioTypeEnum);
            var soundBankType = GetSoundBankTypeFromAudioType(audioTypeEnum);

            if (soundBankType == SoundBankType.DialogueEventBnk)
            {
                var dialogueEventSoundBanks = _audioProjectService.AudioProject.SoundBanks.Where(soundBank => soundBank.Type == SoundBankType.DialogueEventBnk.ToString()).ToList();
                var audioProjectHasDialogueEvents = dialogueEventSoundBanks.Any(soundBank => soundBank.DialogueEvents.Any());

                if (!audioProjectHasDialogueEvents)
                    InitialiseModdedStatesGroups(_audioProjectService.AudioProject.ModdedStates, _audioProjectService.AudioProject.AudioProjectTreeViewItems);

                // Check whether the SounBank exists, if not add it.
                var soundBank = GetSoundBankFromSelectedAudioType(SelectedAudioType, _audioProjectService.AudioProject.SoundBanks);

                if (soundBank == null)
                {
                    soundBank = new SoundBank
                    {
                        Name = audioType,
                        Type = soundBankType.ToString(),
                        DialogueEvents = new ObservableCollection<DialogueEvent>()
                    };

                    _audioProjectService.AudioProject.SoundBanks.Add(soundBank);
                }

                var selectedConfigurationDialogueEvents = GetCheckedDialogueEventsInAudioProjectConfiguration();

                foreach (var dialogueEventName in selectedConfigurationDialogueEvents)
                    soundBank.DialogueEvents.Add(new DialogueEvent { Name = dialogueEventName });
            }

            else if (soundBankType == SoundBankType.ActionEventBnk)
            {
                // Check whether the SounBank exists, if not add it.
                var soundBank = GetSoundBankFromSelectedAudioType(SelectedAudioType, _audioProjectService.AudioProject.SoundBanks);

                if (soundBank == null)
                {
                    soundBank = new SoundBank
                    {
                        Name = audioType,
                        Type = soundBankType.ToString(),
                        ActionEvents = new ObservableCollection<ActionEvent>()
                    };
                }

                _audioProjectService.AudioProject.SoundBanks.Add(soundBank);
            }

            // Sorting.
            SortSoundBanksByName(_audioProjectService.AudioProject.SoundBanks);

            // Update the TreeViewItems with the newly added items.
            UpdateAudioProjectTreeViewItems();

            // Reset data in the UI and clear the previous DataGrids.
            ResetAudioProjectConfiguration();
            ClearDataGrid(AudioProjectEditorDataGrid);
            ClearDataGrid(AudioProjectViewerDataGrid);
            ClearDataGridColumns(_dataGridBuilderName);
            ClearDataGridColumns(_dataGridNameName);
        }

        private HashSet<string> GetCheckedDialogueEventsInAudioProjectConfiguration()
        {
            var selectedDialogueEvents = new HashSet<string>();

            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.IsEnabled == true)
                {
                    var dialogueEventName = RemoveExtraUnderscoresFromString(checkBox.Content.ToString());
                    selectedDialogueEvents.Add(dialogueEventName);
                }
            }

            return selectedDialogueEvents;
        }
    }
}
