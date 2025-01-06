using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor
{
    public class DialogueEventFilter
    {
        public static void ApplyDialogueEventPresetFiltering(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                {
                    if (audioEditorViewModel.SelectedDialogueEventPreset != null)
                    {
                        StoreDialogueEventSoundBankFiltering(selectedSoundBank.Name, audioEditorViewModel.SelectedDialogueEventPreset, audioEditorViewModel.DialogueEventSoundBankFiltering);
                        selectedSoundBank.FilteredBy = $" (Filtered by {audioEditorViewModel.SelectedDialogueEventPreset} preset)";

                        var selectedDialogueEventPresetEnum = GetDialogueEventPreset(audioEditorViewModel.SelectedDialogueEventPreset);
                        AddPresetDialogueEventsToSoundBankTreeViewItems(audioProjectService.AudioProject, selectedSoundBank.Name, selectedDialogueEventPresetEnum, audioEditorViewModel.ShowEditedDialogueEventsOnly);
                    }
                }
            }
        }

        private static void StoreDialogueEventSoundBankFiltering(string soundBank, string selectedDialogueEventPreset, Dictionary<string, string> dialogueEventSoundBankFiltering)
        {
            if (!dialogueEventSoundBankFiltering.TryAdd(soundBank, selectedDialogueEventPreset))
                dialogueEventSoundBankFiltering[soundBank] = selectedDialogueEventPreset;
        }

        public static void HandleDialogueEventsPresetFilter(string soundBankDisplayString, ref ObservableCollection<string> dialogueEventPresets, Dictionary<string, string> dialogueEventSoundBankFiltering, string selectedDialogueEventPreset, ref bool isDialogueEventPresetFilterEnabled)
        {
            var soundBank = GetSoundBank(soundBankDisplayString);
            dialogueEventPresets = new(DialogueEventData
                .Where(dialogueEvent => dialogueEvent.SoundBank == soundBank)
                .SelectMany(dialogueEvent => dialogueEvent.DialogueEventPreset)
                .Select(dialogueEventPreset => GetDisplayString(dialogueEventPreset))
                .Distinct()
                .OrderBy(dialogueEventPreset => dialogueEventPreset == "Show All" ? string.Empty : dialogueEventPreset)
            );

            if (dialogueEventSoundBankFiltering.TryGetValue(soundBankDisplayString, out var storedDialogueEventPreset))
                selectedDialogueEventPreset = storedDialogueEventPreset.ToString();

            isDialogueEventPresetFilterEnabled = true;
        }

        public static void ResetDialogueEventFiltering(Dictionary<string, string> dialogueEventSoundBankFiltering, ref string selectedDialogueEventPreset, IAudioProjectService audioProjectService)
        {
            dialogueEventSoundBankFiltering.Clear();
            selectedDialogueEventPreset = null;
            foreach (var soundBank in audioProjectService.AudioProject.SoundBanks)
                soundBank.FilteredBy = null;
        }

        public static void AddAllDialogueEventsToSoundBankTreeViewItems(AudioProjectData audioProject, bool showEditedDialogueEventsOnly)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.SoundBankTreeViewItems.Clear();

                if (soundBank.DialogueEvents != null)
                {
                    if (showEditedDialogueEventsOnly == true)
                    {
                        var editedDialogueEvents = soundBank.DialogueEvents.Where(dialogueEvent => dialogueEvent.DecisionTree.Count > 0);
                        foreach (var dialogueEvent in editedDialogueEvents)
                            if (!soundBank.SoundBankTreeViewItems.Contains(dialogueEvent))
                                soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                    }
                    else
                    {
                        foreach (var dialogueEvent in soundBank.DialogueEvents)
                            if (!soundBank.SoundBankTreeViewItems.Contains(dialogueEvent))
                                soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                    }
                }
            }
        }

        public static void AddPresetDialogueEventsToSoundBankTreeViewItems(AudioProjectData audioProject, string targetSoundBank, DialogueEventPreset dialogueEventPresetEnum, bool showEditedDialogueEventsOnly)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (soundBank.Name == targetSoundBank)
                {
                    soundBank.SoundBankTreeViewItems.Clear();

                    if (soundBank.DialogueEvents != null)
                    {
                        var presetDialogueEvents = DialogueEventData
                            .Where(dialogueEvent => GetDisplayString(dialogueEvent.SoundBank) == targetSoundBank
                                && dialogueEvent.DialogueEventPreset.Contains(dialogueEventPresetEnum))
                            .Select(dialogueEvent => dialogueEvent.Name);

                        if (showEditedDialogueEventsOnly == true)
                        {
                            var editedDialogueEvents = soundBank.DialogueEvents.Where(dialogueEvent => dialogueEvent.DecisionTree.Count > 0);
                            foreach (var dialogueEvent in editedDialogueEvents)
                                if (presetDialogueEvents.Contains(dialogueEvent.Name) && !(soundBank.SoundBankTreeViewItems.Contains(dialogueEvent)))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                        else
                        {
                            foreach (var dialogueEvent in soundBank.DialogueEvents)
                                if (presetDialogueEvents.Contains(dialogueEvent.Name) && !(soundBank.SoundBankTreeViewItems.Contains(dialogueEvent)))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                    }
                }
            }
        }

        public static void AddEditedDialogueEventsToSoundBankTreeViewItems(AudioProjectData audioProject, Dictionary<string, string> dialogueEventFiltering, bool showEditedDialogueEventsOnly)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.SoundBankTreeViewItems.Clear();

                if (soundBank.DialogueEvents != null)
                {
                    if (showEditedDialogueEventsOnly == true)
                    {
                        var editedDialogueEvents = soundBank.DialogueEvents.Where(dialogueEvent => dialogueEvent.DecisionTree.Count > 0);

                        if (dialogueEventFiltering.Keys.ToList().Contains(soundBank.Name))
                        {
                            var presetDialogueEvents = DialogueEventData
                                .Where(dialogueEventData =>
                                    dialogueEventFiltering.TryGetValue(GetDisplayString(dialogueEventData.SoundBank), out var dialogueEventPreset)
                                    && dialogueEventData.DialogueEventPreset.Contains(GetDialogueEventPreset(dialogueEventPreset)))
                                .Select(dialogueEventData => dialogueEventData.Name);

                            foreach (var dialogueEvent in editedDialogueEvents)
                                if (presetDialogueEvents.Contains(dialogueEvent.Name) && !(soundBank.SoundBankTreeViewItems.Contains(dialogueEvent)))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                        else
                        {
                            foreach (var dialogueEvent in editedDialogueEvents)
                                if (!soundBank.SoundBankTreeViewItems.Contains(dialogueEvent))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                    }
                    else
                    {
                        if (dialogueEventFiltering.Keys.ToList().Contains(soundBank.Name))
                        {
                            var presetDialogueEvents = DialogueEventData
                                .Where(dialogueEventData =>
                                    dialogueEventFiltering.TryGetValue(GetDisplayString(dialogueEventData.SoundBank), out var dialogueEventPreset)
                                    && dialogueEventData.DialogueEventPreset.Contains(GetDialogueEventPreset(dialogueEventPreset)))
                                .Select(dialogueEventData => dialogueEventData.Name);

                            foreach (var dialogueEvent in soundBank.DialogueEvents)
                                if (presetDialogueEvents.Contains(dialogueEvent.Name) && !(soundBank.SoundBankTreeViewItems.Contains(dialogueEvent)))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                        else
                        {
                            foreach (var dialogueEvent in soundBank.DialogueEvents)
                                if (!soundBank.SoundBankTreeViewItems.Contains(dialogueEvent))
                                    soundBank.SoundBankTreeViewItems.Add(dialogueEvent);
                        }
                    }
                }
            }
        }
    }
}
