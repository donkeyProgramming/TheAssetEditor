using Audio.BnkCompiler;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Common;
using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Audio.Utility
{
    public class AudioProjectExporter
    {
        public AudioProjectExporter()
        {
        }

        void AddEventToProject(CAkEvent_v136 wwiseEvent, IAudioRepository repository, CompilerData project)
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
                Name = eventName,
                OverrideId = wwiseEvent.Id,
                Actions = new List<string> { actionName },
            };

            // Actions
            var wwiseActionId = wwiseEvent.Actions.First();
            var wwiseActions = repository.GetHircObject(wwiseActionId.ActionId);
            Guard.IsEqualTo(wwiseActions.Count, 1);

            var wwiseActionInstance = wwiseActions.First() as CAkAction_v136;
            Guard.IsNotNull(wwiseActionInstance);

            // Write Action
            var projectAction = new Action()
            {
                Name = actionName,
                OverrideId = wwiseActionInstance.Id,
                ChildId = soundName,
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
                Name = soundName,
                OverrideId = wwiseSoundInstance.Id,
                Path = $"Audio\\WWise\\{wwiseSoundInstance.AkBankSourceData.akMediaInformation.SourceId}.wem",
            };

            project.Events.Add(projectEvent);
            project.Actions.Add(projectAction);
            project.GameSounds.Add(projectSound);
        }

        public CompilerData CreateFromRepository(IAudioRepository repository, string bnkName)
        {
            var project = new CompilerData();
            project.ProjectSettings.BnkName = bnkName;

            var events = repository.GetAllOfType<CAkEvent_v136>();
            foreach (var wwiseEvent in events)
                AddEventToProject(wwiseEvent, repository, project);

            var mixers = repository.GetAllOfType<CAkActorMixer_v136>();
            AddMixersToProject(mixers, repository, project);

            return project;
        }

        void AddMixersToProject(List<CAkActorMixer_v136> mixers, IAudioRepository repository, CompilerData project)
        {
            // Revese the list, to get better ordering.
            mixers.Reverse();
            var mixerNames = GenerateMixerNames(mixers);

            foreach (var mixer in mixers)
            {
                var childen = mixer.Children.ChildIdList;
                var childHircs = childen.Select(x => repository.GetHircObject(x)).SelectMany(x => x).ToList();
                var audioChildren = childHircs.Where(x => x is CAkSound_v136).ToList();
                var mixerChildren = childHircs.Where(x => x is CAkActorMixer_v136).ToList();

                var outputMixer = new ActorMixer
                {
                    Name = mixerNames[mixer.Id],
                    DirectParentId = "Master",
                    OverrideId = mixer.Id,
                    Sounds = audioChildren.Select(x => project.GameSounds.First(sound => sound.OverrideId == x.Id).Name).ToList(), // we have to match on this
                    ActorMixerChildren = mixerChildren.Select(x => mixerNames[x.Id]).ToList(),
                };

                project.ActorMixers.Add(outputMixer);
            }
        }

        Dictionary<uint, string> GenerateMixerNames(List<CAkActorMixer_v136> mixers)
        {
            var mixerCounter = 0;
            var nameDictionary = new Dictionary<uint, string>();
            foreach (var mixer in mixers)
            {
                if (mixer.NodeBaseParams.DirectParentID == 0)
                    nameDictionary.Add(mixer.Id, "RootMixer");
                else

                    nameDictionary.Add(mixer.Id, $"ActorMixer_{mixerCounter++}");
            }

            // Ensure we have one name for each mixer
            Guard.Equals(mixers.Count(), nameDictionary.Count());

            return nameDictionary;
        }

        public void CreateFromRepositoryToFile(IAudioRepository repository, string bnkName, string path = "audioProject.json")
        {
            var project = CreateFromRepository(repository, bnkName);
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            DirectoryHelper.EnsureCreated(path);
            File.WriteAllText(path, json);
        }
    }
}

