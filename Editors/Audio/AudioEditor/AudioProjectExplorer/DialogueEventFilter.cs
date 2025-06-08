using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.GameSettings.Warhammer3;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class DialogueEventFilter
    {
        public static void ApplyDialogueEventPresetFiltering(IAudioEditorService audioEditorService)
        {
            var selectedExplorerNode = audioEditorService.SelectedExplorerNode;
            if (selectedExplorerNode.IsDialogueEventSoundBank())
            {
                var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioEditorService.AudioProject, audioEditorService.SelectedExplorerNode.Name);

                var presetFilter = audioEditorService.SelectedDialogueEventPreset;

                if (presetFilter != null)
                {
                    selectedExplorerNode.PresetFilter = presetFilter;

                    if (presetFilter != DialogueEventPreset.ShowAll)
                        selectedExplorerNode.PresetFilterDisplayText = $" (Filtered by {GetDialogueEventPresetDisplayString(presetFilter)} preset)";
                    else
                        selectedExplorerNode.PresetFilterDisplayText = null;
                }

                TreeBuilder.AddFilteredDialogueEventsToSoundBankTreeViewItems(audioEditorService, soundBank.Name, presetFilter);
            }
        }

        public static void HandleDialogueEventsPresetFilter(AudioProjectExplorerViewModel audioProjectExplorerViewModel, IAudioEditorService audioEditorService, string soundBankName)
        {
            SetDialogueEventPresets(audioProjectExplorerViewModel, soundBankName);

            SetSelectedDialogueEventPreset(audioEditorService);

            audioProjectExplorerViewModel.IsDialogueEventPresetFilterEnabled = true;
        }

        private static void SetDialogueEventPresets(AudioProjectExplorerViewModel audioProjectExplorerViewModel, string soundBankName)
        {
            var soundBankSubtype = SoundBanks.GetSoundBankSubtype(soundBankName);

            var dialogueEventPresets = new ObservableCollection<DialogueEventPreset>(DialogueEventData
                .Where(dialogueEvent => dialogueEvent.SoundBank == soundBankSubtype)
                .SelectMany(dialogueEvent => dialogueEvent.DialogueEventPreset)
                .Distinct());

            audioProjectExplorerViewModel.DialogueEventPresets = dialogueEventPresets;
        }

        private static void SetSelectedDialogueEventPreset(IAudioEditorService audioEditorService)
        {
            var soundBank = TreeNode.GetNodeFromName(audioEditorService.AudioProjectTree, audioEditorService.SelectedExplorerNode.Name);
            if (soundBank.PresetFilter != DialogueEventPreset.ShowAll && soundBank.PresetFilter != null)
                audioEditorService.SelectedDialogueEventPreset = soundBank.PresetFilter;
            else
                audioEditorService.SelectedDialogueEventPreset = null;
        }
    }
}
