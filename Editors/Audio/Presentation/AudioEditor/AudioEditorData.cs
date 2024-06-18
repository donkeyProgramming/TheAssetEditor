using System.Collections.Generic;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioProject
    {
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public Dictionary<string, List<AudioProjectItem>> AudioProjectItems { get; set; } = new Dictionary<string, List<AudioProjectItem>>();

        public class ProjectSettings
        {
            public string BnkName { get; set; }
            public string Language { get; set; }
        }

        public class AudioProjectItem
        {
            public Dictionary<string, string> StatePath { get; set; }
            public List<string> Sounds { get; set; }

            public AudioProjectItem()
            {
                StatePath = new Dictionary<string, string>();
                Sounds = new List<string>();
            }
        }

        public void AddAudioEditorItem(string eventName, Dictionary<string, string> statePath)
        {
            if (!AudioProjectItems.ContainsKey(eventName))
                AudioProjectItems[eventName] = new List<AudioProjectItem>();

            var audioEditorItem = new AudioProjectItem
            {
                StatePath = statePath,
                Sounds = new List<string>()
            };

            AudioProjectItems[eventName].Add(audioEditorItem);
        }
    }
}
