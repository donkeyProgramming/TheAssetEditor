using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using TreeNode = Editors.Audio.AudioEditor.AudioProjectExplorer.TreeNode;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorService
    {
        public AudioProject AudioProject { get; set; }
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; set; }
        public AudioSettingsViewModel AudioSettingsViewModel { get; set; }
        public Dictionary<string, List<string>> ModdedStatesByStateGroupLookup { get; set; }
        public Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; }
        public Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; }
        public void SaveAudioProject();
        public void LoadAudioProject(AudioEditorViewModel audioEditorViewModel);
        public void InitialiseAudioProject(string fileName, string directory, string language);
        public void CompileAudioProject();
        public void BuildModdedStatesByStateGroupLookup(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> moddedStatesByStateGroupLookup);
        public void ResetAudioProject();
        public TreeNode GetSelectedExplorerNode();
        public ObservableCollection<Dictionary<string, string>> GetEditorDataGrid();
        public ObservableCollection<Dictionary<string, string>> GetViewerDataGrid();
        public ObservableCollection<Dictionary<string, string>> GetSelectedViewerRows();
    }
}
