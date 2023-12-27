using Audio.BnkCompiler;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Common;
using CommunityToolkit.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Audio.Utility
{
    public class AudioProjectExporterSimple
    {
        /*
        public AudioProjectExporterSimple()
        {
        }

        void AddEventToProject(CAkEvent_v136 wwiseEvent, IAudioRepository repository, CompilerInputProject project)
        {
            Guard.IsNotNull(wwiseEvent);
            Guard.IsEqualTo(wwiseEvent.Actions.Count, 1);

            var eventName = repository.GetNameFromHash(wwiseEvent.Id, out var found);
            Guard.IsTrue(found);

            // Actions
            var wwiseActionId = wwiseEvent.Actions.First();
            var wwiseActions = repository.GetHircObject(wwiseActionId.ActionId);
            Guard.IsEqualTo(wwiseActions.Count, 1);

            var wwiseActionInstance = wwiseActions.First() as CAkAction_v136;
            Guard.IsNotNull(wwiseActionInstance);

            // Sound
            var wwiseGameSoundId = wwiseActionInstance.GetChildId();
            var wwiseGameSounds = repository.GetHircObject(wwiseGameSoundId);
            Guard.IsEqualTo(wwiseGameSounds.Count, 1);
            var wwiseSoundInstance = wwiseGameSounds.First() as CAkSound_v136;
            Guard.IsNotNull(wwiseSoundInstance);

            // Write Event
            var projectEvent = new CompilerInputProject.Event()
            {
                Name = eventName,
                Sounsds = $"Audio\\WWise\\{wwiseSoundInstance.AkBankSourceData.akMediaInformation.SourceId}.wem",
            };

            project.Events.Add(projectEvent);
        }

        public CompilerInputProject CreateFromRepository(IAudioRepository repository, string bnkName)
        {
            var project = new CompilerInputProject();
            project.Settings.BnkName = bnkName;

            var events = repository.GetAllOfType<CAkEvent_v136>();
            foreach (var wwiseEvent in events)
                AddEventToProject(wwiseEvent, repository, project);

            return project;
        }

        public void CreateFromRepositoryToFile(IAudioRepository repository, string bnkName, string path = "audioProject.json")
        {
            var project = CreateFromRepository(repository, bnkName);
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            DirectoryHelper.EnsureCreated(path);
            File.WriteAllText(path, json);
        }
        */
    }
}

