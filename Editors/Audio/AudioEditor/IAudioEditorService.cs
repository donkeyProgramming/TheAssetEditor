using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorService
    {
        AudioProject AudioProject { get; set; }
        string AudioProjectFileName { get; set; }
        string AudioProjectDirectory { get; set; }
        Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; }
        Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; }
        Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; }
        void SaveAudioProject();
        void LoadAudioProject(AudioEditorViewModel audioEditorViewModel);
        void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language);
        void CompileAudioProject();
        void BuildStateGroupsWithModdedStatesRepository(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedStatesRepository);
        void ResetAudioProject();
    }
}
