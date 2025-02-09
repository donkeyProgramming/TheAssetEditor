using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public class AudioProjectDataServiceParameters
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public IAudioProjectService AudioProjectService { get; set; }
        public IAudioRepository AudioRepository { get; set; }
        public SoundBank SoundBank { get; set; }
        public DialogueEvent DialogueEvent { get; set; }
        public StateGroup StateGroup { get; set; }
        public Dictionary<string, string> AudioProjectEditorRow { get; set; }
    }
}
