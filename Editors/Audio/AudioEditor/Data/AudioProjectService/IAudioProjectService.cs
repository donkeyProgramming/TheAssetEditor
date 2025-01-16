using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.Data.AudioProjectService
{
    public interface IAudioProjectService
    {
        AudioProjectData AudioProject { get; set; }
        string AudioProjectFileName { get; set; }
        string AudioProjectDirectory { get; set; }
        Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; }
        Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; }
        void SaveAudioProject(IPackFileService packFileService);
        void LoadAudioProject(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioRepository audioRepository, IStandardDialogs packFileUiProvider);
        void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language);
        void BuildStateGroupsWithModdedStatesRepository(ObservableCollection<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedStatesRepository);
        void ResetAudioProject();
    }
}
