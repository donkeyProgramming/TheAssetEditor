using System.Collections.Generic;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorData
    {
        private static readonly AudioEditorData _instance = new();

        public static AudioEditorData Instance => _instance;

        public Dictionary<string, List<Dictionary<string, object>>> EventsData { get; set; } = [];

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = [];

        public string SelectedAudioProjectEvent { get; set; }

        private AudioEditorData()
        {
        }
    }
}
