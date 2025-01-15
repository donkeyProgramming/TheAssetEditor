using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class DialogueEventFilter
    {
        public static void ApplyDialogueEventPresetFiltering(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                {
                    if (audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset != null)
                    {
                        StoreDialogueEventSoundBankFiltering(selectedSoundBank.Name, audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset, audioEditorViewModel.AudioProjectExplorerViewModel.DialogueEventSoundBankFiltering);
                        selectedSoundBank.FilteredBy = $" (Filtered by {audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset} preset)";

                        var selectedDialogueEventPresetEnum = GetDialogueEventPreset(audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset);
                        TreeViewBuilder.AddPresetDialogueEventsToSoundBankTreeViewItems(audioProjectService.AudioProject, selectedSoundBank.Name, selectedDialogueEventPresetEnum, audioEditorViewModel.AudioProjectExplorerViewModel.ShowEditedDialogueEventsOnly);
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
    }
}
