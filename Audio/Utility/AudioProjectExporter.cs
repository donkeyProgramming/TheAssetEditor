using Audio.AudioEditor;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommunityToolkit.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;

namespace Audio.Utility
{
    public class AudioProjectExporter
    {
        void AddItemToProject(CAkEvent_v136 wwiseEvent, IAudioRepository repository, AudioInputProject project)
        {
            Guard.IsNotNull(wwiseEvent);
            Guard.IsEqualTo(wwiseEvent.Actions.Count, 1);

            var eventName = repository.GetNameFromHash(wwiseEvent.Id, out var found);
            var actionName = $"{eventName}_Action";
            var soundName = $"{eventName}_GameSound";
            Guard.IsTrue(found);

            // Write Event
            var projectEvent = new Event()
            {
                Id = eventName,
                Actions = new List<string> { actionName },
                AudioBus = "Master"
            };
            project.Events.Add(projectEvent);

            // Actions
            var wwiseActionId = wwiseEvent.Actions.First();
            var wwiseActions = repository.GetHircObject(wwiseActionId.ActionId);
            Guard.IsEqualTo(wwiseActions.Count, 1);

            var wwiseActionInstance = wwiseActions.First() as CAkAction_v136;
            Guard.IsNotNull(wwiseActionInstance);

            // Write Action
            var projectAction = new Action()
            {
                Id = actionName,
                ChildId = soundName,
                OverrideId = wwiseActionInstance.Id,
                Type = "Play"
            };

            // Sound
            var wwiseGameSoundId = wwiseActionInstance.GetChildId();
            var wwiseGameSounds = repository.GetHircObject(wwiseGameSoundId);
            Guard.IsEqualTo(wwiseGameSounds.Count, 1);
            var wwiseSoundInstance = wwiseGameSounds.First() as CAkSound_v136;
            Guard.IsNotNull(wwiseSoundInstance);

            // Write sound
            var projectSound = new GameSound()
            {
                ConvertToWem = false,
                Id = soundName,
                OverrideId = wwiseSoundInstance.Id,
                Path = $"Audio\\WWise\\{wwiseSoundInstance.AkBankSourceData.akMediaInformation.SourceId}.wem",
                SourceType = "PackFile"
            };

            project.Events.Add(projectEvent);
            project.Actions.Add(projectAction);
            project.GameSounds.Add(projectSound);
        }

        public AudioInputProject CreateFromRepository(IAudioRepository repository)
        {
            var events = repository.GetAllOfType<CAkEvent_v136>();
            
            //var fxShareSets = repository.GetAllOfType<CAkFxShareSet_v136>();
            //var mixers = repository.GetAllOfType<CAkActorMixer_v136>();

            var project = new AudioInputProject();
            foreach (var wwiseEvent in events)
                AddItemToProject(wwiseEvent, repository, project);

            return project;
        }

        public void CreateFromRepository(IAudioRepository repository, string filename = "audioProject.json")
        { 
            var project = CreateFromRepository(repository);
            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText($"D:\\Research\\Audio\\{filename}", json);
        }
    }
}

