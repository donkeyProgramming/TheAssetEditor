using Audio.AudioEditor;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommunityToolkit.Diagnostics;
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
        private readonly bool _useSoundIdFromBnk;
        private readonly bool _useMixerIdFromBnk;
        private readonly bool _useActionIdFromBnk;

        public AudioProjectExporter(bool useSoundIdFromBnk = true, bool useMixerIdFromBnk = true, bool useActionIdFromBnk = true)
        {
            _useSoundIdFromBnk = useSoundIdFromBnk;
            _useMixerIdFromBnk = useMixerIdFromBnk;
            _useActionIdFromBnk = useActionIdFromBnk;
        }

        void AddEventToProject(CAkEvent_v136 wwiseEvent, IAudioRepository repository, AudioInputProject project)
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
                OverrideId = wwiseEvent.Id,
                Actions = new List<string> { actionName },
                AudioBus = "Master"
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
                Id = actionName,
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
                Id = soundName,
                OverrideId = wwiseSoundInstance.Id,
                Path = $"Audio\\WWise\\{wwiseSoundInstance.AkBankSourceData.akMediaInformation.SourceId}.wem",
            };

            project.Events.Add(projectEvent);
            project.Actions.Add(projectAction);
            project.GameSounds.Add(projectSound);
        }

        public AudioInputProject CreateFromRepository(IAudioRepository repository)
        {
            var project = new AudioInputProject();

            var events = repository.GetAllOfType<CAkEvent_v136>();
            foreach (var wwiseEvent in events)
                AddEventToProject(wwiseEvent, repository, project);

            var mixers = repository.GetAllOfType<CAkActorMixer_v136>();
            AddMixersToProject(mixers, repository, project);

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

        public void CreateFromRepository(IAudioRepository repository, string path = "audioProject.json")
        { 
            var project = CreateFromRepository(repository);
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions() { WriteIndented = true, IgnoreNullValues = true});
            DirectoryHelper.EnsureCreated(path);
            File.WriteAllText(path, json);
        }
    }
}

