using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.Data.AudioProjectService
{
    public interface IAudioProjectService
    {
        AudioProjectDataModel AudioProject { get; set; }
        string AudioProjectFileName { get; set; }
        string AudioProjectDirectory { get; set; }
        Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; }
        Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; }
        Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; }
        void SaveAudioProject(IPackFileService packFileService);
        void LoadAudioProject(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioRepository audioRepository, IStandardDialogs packFileUiProvider);
        void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language);
        void CompileAudioProject(ApplicationSettingsService applicationSettingsService);
        void BuildStateGroupsWithModdedStatesRepository(ObservableCollection<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedStatesRepository);
        void ResetAudioProject();
    }
}
