using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.GameSettings.Warhammer3;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class DialogueEventFilter
    {
        public static void ApplyDialogueEventPresetFiltering(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var audioProjectTreeNode = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            if (audioProjectTreeNode.NodeType == NodeType.DialogueEventSoundBank)
            {
                var soundBank = DataHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

                var presetFilter = audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset;

                if (presetFilter != null)
                {
                    audioProjectTreeNode.PresetFilter = presetFilter;

                    if (presetFilter != DialogueEventPreset.ShowAll)
                        audioProjectTreeNode.PresetFilterDisplayText = $" (Filtered by {GetDialogueEventPresetDisplayString(presetFilter)} preset)";
                    else
                        audioProjectTreeNode.PresetFilterDisplayText = null;
                }

                TreeBuilder.AddFilteredDialogueEventsToSoundBankTreeViewItems(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel, soundBank.Name, presetFilter);
            }
        }

        public static void HandleDialogueEventsPresetFilter(AudioProjectExplorerViewModel audioProjectExplorerViewModel, IAudioProjectService audioProjectService, string soundBankName)
        {
            SetDialogueEventPresets(audioProjectExplorerViewModel, soundBankName);

            SetSelectedDialogueEventPreset(audioProjectExplorerViewModel, audioProjectService);

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

        private static void SetSelectedDialogueEventPreset(AudioProjectExplorerViewModel audioProjectExplorerViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = DataHelpers.GetAudioProjectTreeNodeFromName(audioProjectExplorerViewModel.AudioProjectTree, audioProjectExplorerViewModel.GetSelectedAudioProjectNodeName());
            if (soundBank.PresetFilter != DialogueEventPreset.ShowAll && soundBank.PresetFilter != null)
                audioProjectExplorerViewModel.SelectedDialogueEventPreset = soundBank.PresetFilter;
            else
                audioProjectExplorerViewModel.SelectedDialogueEventPreset = null;
        }
    }
}
