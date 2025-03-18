using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorService
    {
        public AudioProject AudioProject { get; set; }
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public TreeNode SelectedAudioProjectTreeNode { get; set; }
        public ObservableCollection<Dictionary<string, string>> AudioProjectEditorDataGrid { get; set; }
        public ObservableCollection<Dictionary<string, string>> AudioProjectViewerDataGrid { get; set; }
        public Dictionary<string, List<string>> ModdedStatesByStateGroupLookup { get; set; }
        public Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; }
        public Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; }
        public void SaveAudioProject();
        public void LoadAudioProject(AudioEditorViewModel audioEditorViewModel);
        public void InitialiseAudioProject(string fileName, string directory, string language);
        public void CompileAudioProject();
        public void BuildModdedStatesByStateGroupLookup(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> moddedStatesByStateGroupLookup);
        public void ResetAudioProject();
    }
}
