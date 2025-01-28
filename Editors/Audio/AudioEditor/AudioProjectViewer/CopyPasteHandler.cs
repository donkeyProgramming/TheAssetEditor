using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public class CopyPasteHandler
    {
        public static void CopyDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            // Initialise new data rather than setting it directly so it's not referencing the original object which for example gets cleared when a new Event is selected
            audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows = new ObservableCollection<Dictionary<string, object>>();

            foreach (var item in audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows)
                audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Add(new Dictionary<string, object>(item));

            SetPasteEnablement(audioEditorViewModel, audioRepository, audioProjectService);
        }

        public static void PasteDialogueEventRows(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService, DialogueEvent selectedDialogueEvent)
        {
            foreach (var copiedDataGridRow in audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows)
            {
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(copiedDataGridRow);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioProjectEditorRow = copiedDataGridRow,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(selectedDialogueEvent);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
            }

            SetPasteEnablement(audioEditorViewModel, audioRepository, audioProjectService);
        }

        public static void SetCopyEnablement(AudioEditorViewModel audioEditorViewModel)
        {
            if (audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows != null)
                audioEditorViewModel.AudioProjectViewerViewModel.IsCopyEnabled = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.Any();
        }

        public static void SetPasteEnablement(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            if (!audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Any())
            {
                audioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows
                .Any(copiedRow => audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid
                .Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
                var dialogueEventStateGroups = audioRepository
                    .DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name]
                    .Select(kvp => AudioProjectHelpers.AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedDataGridRowStateGroups = audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows[0]
                    .Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay" && kvp.Key != "AudioSettings")
                    .Select(kvp => kvp.Key)
                    .ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                audioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }
    }
}
