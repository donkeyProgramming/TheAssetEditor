using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public class AudioEditorData
    {
        private static readonly AudioEditorData _instance = new();

        public static AudioEditorData Instance => _instance;

        public string AudioProjectFileNameInstance { get; set; }

        public Dictionary<string, List<Dictionary<string, object>>> AudioProjectDataInstance { get; set; } = [];

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = [];

        public string SelectedAudioProjectEventInstance { get; set; }

        private AudioEditorData()
        {
        }
    }
}
