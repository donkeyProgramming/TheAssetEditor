using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataService;

namespace Editors.Audio.AudioEditor
{
    public class CopyPasteHandler
    {
        public static void CopyDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            // Initialise new data rather than setting it directly so it's not referencing the original object which for example gets cleared when a new Event is selected
            audioEditorViewModel.CopiedDataGridRows = new ObservableCollection<Dictionary<string, object>>();

            foreach (var item in audioEditorViewModel.SelectedDataGridRows)
                audioEditorViewModel.CopiedDataGridRows.Add(new Dictionary<string, object>(item));

            SetIsPasteEnabled(audioEditorViewModel, audioRepository, audioProjectService);
        }

        public static void PasteDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService, DialogueEvent selectedDialogueEvent)
        {
            foreach (var copiedDataGridRow in audioEditorViewModel.CopiedDataGridRows)
            {
                audioEditorViewModel.AudioProjectEditorFullDataGrid.Add(copiedDataGridRow);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioProjectEditorRow = copiedDataGridRow,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
            }

            SetIsPasteEnabled(audioEditorViewModel, audioRepository, audioProjectService);
        }

        public static void SetIsCopyEnabled(AudioEditorViewModel audioEditorViewModel)
        {
            if (audioEditorViewModel.SelectedDataGridRows != null)
                audioEditorViewModel.IsCopyEnabled = audioEditorViewModel.SelectedDataGridRows.Any();
        }

        public static void SetIsPasteEnabled(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            if (!audioEditorViewModel.CopiedDataGridRows.Any())
            {
                audioEditorViewModel.IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = audioEditorViewModel.CopiedDataGridRows
                .Any(copiedRow => audioEditorViewModel.AudioProjectEditorFullDataGrid
                .Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                var dialogueEventStateGroups = audioRepository
                    .DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[selectedDialogueEvent.Name]
                    .Select(kvp => AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedDataGridRowStateGroups = audioEditorViewModel.CopiedDataGridRows[0]
                    .Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay" && kvp.Key != "AudioSettings")
                    .Select(kvp => kvp.Key)
                    .ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                audioEditorViewModel.IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }
    }
}
