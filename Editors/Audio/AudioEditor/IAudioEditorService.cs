using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using TreeNode = Editors.Audio.AudioEditor.AudioProjectExplorer.TreeNode;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorService
    {
        AudioProject AudioProject { get; set; }
        TreeNode SelectedExplorerNode { get; set; }
        ObservableCollection<AudioFile> AudioFiles { get; set; }
        IAudioSettings AudioSettings { get; set; }





        AudioEditorViewModel AudioEditorViewModel { get; set; }
        AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        AudioProjectViewerViewModel AudioProjectViewerViewModel { get; set; }
        AudioSettingsViewModel AudioSettingsViewModel { get; set; }
        Dictionary<string, List<string>> ModdedStatesByStateGroupLookup { get; set; }
        void SaveAudioProject(AudioProject audioProject, string audioProjectFileName, string audioProjectDirectoryPath);
        void LoadAudioProject(AudioEditorViewModel audioEditorViewModel);
        void InitialiseAudioProject(string fileName, string directory, string language);
        void CompileAudioProject();
        void BuildModdedStatesByStateGroupLookup(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> moddedStatesByStateGroupLookup);
        void ResetAudioProject();
        DataTable GetEditorDataGrid();
        DataTable GetViewerDataGrid();
        List<DataRow> GetSelectedViewerRows();
    }
}
