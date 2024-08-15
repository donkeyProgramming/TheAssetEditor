using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public class AudioEditorData
    {
        private static readonly AudioEditorData _instance = new();

        public static AudioEditorData AudioEditorInstance => _instance;

        public string AudioProjectFileName { get; set; }

        public string CustomStatesFilePath { get; set; }

        public Dictionary<string, List<Dictionary<string, object>>> AudioProjectData { get; set; } = [];

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = [];

        public string SelectedAudioProjectEvent { get; set; }

        private AudioEditorData()
        {
        }

        public void ResetAudioEditorData()
        {
            AudioProjectFileName = null;
            CustomStatesFilePath = null;
            AudioProjectData.Clear();
            StateGroupsWithCustomStates.Clear();
            SelectedAudioProjectEvent = null;
        }
    }
}
