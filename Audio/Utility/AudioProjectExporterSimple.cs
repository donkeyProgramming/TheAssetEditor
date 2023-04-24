using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Audio.Utility
{
    public class AudioProjectExporterSimple
    {

        public AudioProjectExporterSimple()
        {
        }

        void AddEventToProject(CAkEvent_v136 wwiseEvent, IAudioRepository repository, AudioInputProject project)
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
            var projectEvent = new SimpleEvent()
            {
                Id = eventName,
                SoundFile = $"Audio\\WWise\\{wwiseSoundInstance.AkBankSourceData.akMediaInformation.SourceId}.wem",
            };

            project.SimpleEvents.Add(projectEvent);
        }

        public AudioInputProject CreateFromRepository(IAudioRepository repository, string bnkName)
        {
            var project = new AudioInputProject();
            project.ProjectSettings.BnkName = bnkName;

            var events = repository.GetAllOfType<CAkEvent_v136>();
            foreach (var wwiseEvent in events)
                AddEventToProject(wwiseEvent, repository, project);

            return project;
        }

        void AddMixersToProject(List<CAkActorMixer_v136> mixers, IAudioRepository repository, AudioInputProject project)
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
                    Id = mixerNames[mixer.Id],
                    AudioBus = "Master",
                    OverrideId = mixer.Id,
                    Sounds = audioChildren.Select(x => project.GameSounds.First(sound => sound.OverrideId == x.Id).Id).ToList(), // we have to match on this
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
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions() { WriteIndented = true, IgnoreNullValues = true});
            DirectoryHelper.EnsureCreated(path);
            File.WriteAllText(path, json);
        }
    }
}

