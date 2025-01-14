using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor
{
    public class ActionEventSoundBanksTreeViewWrapper
    {
        public ObservableCollection<SoundBank> ActionEventSoundBanks { get; set; }
        public static string Name => "Action Events";
    }

    public class DialogueEventSoundBanksTreeViewWrapper
    {
        public ObservableCollection<SoundBank> DialogueEventSoundBanks { get; set; }
        public static string Name => "Dialogue Events";
    }

    public class StatesTreeViewWrapper
    {
        public ObservableCollection<StateGroup> StateGroups { get; set; }
        public static string Name => "States";
    }

    public class TreeViewBuilder
    {
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

        public static void AddAllSoundBanksToTreeViewItemsWrappers(IAudioProjectService audioProjectService)
        {
            audioProjectService.AudioProject.AudioProjectTreeViewItems.Clear();

            var actionEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                .ToList();

            if (actionEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ActionEventSoundBanksTreeViewWrapper
                {
                    ActionEventSoundBanks = new ObservableCollection<SoundBank>(actionEventSoundBanks)
                });
            }

            var dialogueEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                .ToList();

            if (dialogueEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new DialogueEventSoundBanksTreeViewWrapper
                {
                    DialogueEventSoundBanks = new ObservableCollection<SoundBank>(dialogueEventSoundBanks)
                });
            }

            if (audioProjectService.AudioProject.States.Any())
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new StatesTreeViewWrapper
                {
                    StateGroups = audioProjectService.AudioProject.States
                });
            }
        }

        public static void AddEditedSoundBanksToAudioProjectTreeViewItemsWrappers(IAudioProjectService audioProjectService)
        {
            audioProjectService.AudioProject.AudioProjectTreeViewItems.Clear();

            var actionEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString()
                    && soundBank.ActionEvents.Count > 0)
                .ToList();

            if (actionEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ActionEventSoundBanksTreeViewWrapper
                {
                    ActionEventSoundBanks = new ObservableCollection<SoundBank>(actionEventSoundBanks)
                });
            }

            var dialogueEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString()
                    && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.DecisionTree.Count > 0))
                .ToList();

            if (dialogueEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new DialogueEventSoundBanksTreeViewWrapper
                {
                    DialogueEventSoundBanks = new ObservableCollection<SoundBank>(dialogueEventSoundBanks)
                });
            }

            if (audioProjectService.AudioProject.States.Any())
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new StatesTreeViewWrapper
                {
                    StateGroups = audioProjectService.AudioProject.States
                });
            }
        }
    }
}
