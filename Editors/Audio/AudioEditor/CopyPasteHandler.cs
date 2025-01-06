using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManager;

namespace Editors.Audio.AudioEditor
{
    public class CopyPasteHandler
    {
        public static void CopyDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroups)
        {
            // Initialise new data rather than setting it directly so it's not referencing the original object which for example gets cleared when a new Event is selected
            audioEditorViewModel.CopiedDataGridRows = new ObservableCollection<Dictionary<string, object>>();

            foreach (var item in audioEditorViewModel.SelectedDataGridRows)
                audioEditorViewModel.CopiedDataGridRows.Add(new Dictionary<string, object>(item));

            SetIsPasteEnabled(audioEditorViewModel, audioProjectService, dialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        public static void PasteDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, DialogueEvent selectedDialogueEvent, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroups)
        {
            foreach (var copiedDataGridRow in audioEditorViewModel.CopiedDataGridRows)
            {
                audioEditorViewModel.AudioProjectEditorFullDataGrid.Add(copiedDataGridRow);
                AddRowDataToDialogueEvent(copiedDataGridRow, selectedDialogueEvent, dialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
            }

            SetIsPasteEnabled(audioEditorViewModel, audioProjectService, dialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        public static void SetIsPasteEnabled(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository)
        {
            if (audioEditorViewModel.CopiedDataGridRows.Count == 0)
                return;

            var areAnyCopiedRowsInDataGrid = audioEditorViewModel.CopiedDataGridRows.Any(copiedRow => audioEditorViewModel.AudioProjectEditorFullDataGrid.Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                var dialogueEventStateGroups = new List<string>();

                var stateGroupsWithQualifiers = dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[selectedDialogueEvent.Name];
                foreach (var kvp in stateGroupsWithQualifiers)
                {
                    var stateGroupWithQualifier = AddExtraUnderscoresToString(kvp.Key);
                    dialogueEventStateGroups.Add(stateGroupWithQualifier);
                }

                var copiedDataGridRowStateGroups = new List<string>();
                foreach (var kvp in audioEditorViewModel.CopiedDataGridRows[0])
                {
                    if (kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay")
                        copiedDataGridRowStateGroups.Add(kvp.Key);
                }

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);
                if (!areStateGroupsEqual || areAnyCopiedRowsInDataGrid)
                    audioEditorViewModel.IsPasteEnabled = false;
                else if (areStateGroupsEqual && !areAnyCopiedRowsInDataGrid)
                    audioEditorViewModel.IsPasteEnabled = true;
            }
        }
    }
}
