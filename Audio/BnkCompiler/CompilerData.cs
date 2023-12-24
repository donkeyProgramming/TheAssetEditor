using Audio.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Audio.BnkCompiler
{
    public abstract class IAudioProjectHircItem
    {
        public string Name { get; set; }
        public uint OverrideId { get; set; } = 0;
        [JsonIgnore]
        public uint SerializationId { get; set; }
    }

    public class Event : IAudioProjectHircItem
    {
        public List<string> Actions { get; set; }
    }

    public class Action : IAudioProjectHircItem
    {
        public string ChildId { get; set; }
        public string Type { get; set; }
    }

    public class GameSound : IAudioProjectHircItem
    {
        public string Path { get; set; }
        public string StatePropNum_Priority { get; set; } = null;
        public string UserAuxSendVolume0 { get; set; } = null;
        public string InitialDelay { get; set; } = null;
    }

    public class ActorMixer : IAudioProjectHircItem
    {
        public string DirectParentId { get; set; } = null;
        public string OverrideBusId { get; set; } = null;
        public string StatePropNum_Priority { get; set; } = null;
        public string UserAuxSendVolume0 { get; set; } = null;
        public string InitialDelay { get; set; } = null;
        public List<string> Sounds { get; set; } = new List<string>();
        public List<string> ActorMixerChildren { get; set; } = new List<string>();
    }

    public class ProjectSettings
    {
        public int Version { get; set; } = 1;
        public string ProjectType { get; set; }
        public string OutputGame { get; set; } = CompilerConstants.Game_Warhammer3;
        public string BnkName { get; set; }
        public string Language { get; internal set; }
    }

    public class CompilerData
    {
        private List<IAudioProjectHircItem> _allProjectItems = new List<IAudioProjectHircItem>();

        public ProjectSettings ProjectSettings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Action> Actions { get; set; } = new List<Action>();
        public List<GameSound> GameSounds { get; set; } = new List<GameSound>();
        public List<ActorMixer> ActorMixers { get; set; } = new List<ActorMixer>();

        public void PreperForCompile(bool allowOverrideIdForActions, bool allowOverrideIdForMixers, bool allowOverrideIdForSounds)
        {
            _allProjectItems.AddRange(Events);
            _allProjectItems.AddRange(Actions);
            _allProjectItems.AddRange(GameSounds);
            _allProjectItems.AddRange(ActorMixers);

            // Compute the write ids
            Events.ForEach(x => Process(x, false, WWiseHash.Compute));
            Actions.ForEach(x => Process(x, allowOverrideIdForActions, WWiseHash.Compute));
            ActorMixers.ForEach(x => Process(x, allowOverrideIdForMixers, WWiseHash.Compute30));
            GameSounds.ForEach(x => Process(x, allowOverrideIdForSounds, WWiseHash.Compute30));
        }

        public uint GetHircItemIdFromName(string name)
        {
            return _allProjectItems.First(x => x.Name == name).SerializationId;
        }

        public IAudioProjectHircItem GetActionMixerForSound(string soundName)
        {
            var mixers = _allProjectItems.Where(x => x is ActorMixer).Cast<ActorMixer>().ToList();
            var mixer = mixers.Where(x => x.Sounds.Contains(soundName)).ToList();
            return mixer.FirstOrDefault();
        }

        public IAudioProjectHircItem GetActionMixerParentForActorMixer(string soundName)
        {
            var mixers = _allProjectItems.Where(x => x is ActorMixer).Cast<ActorMixer>().ToList();
            var mixer = mixers.Where(x => x.ActorMixerChildren.Contains(soundName)).ToList();
            return mixer.FirstOrDefault();
        }

        public uint ConvertStringToWWiseId(string id) => WWiseHash.Compute(id);

        void Process(IAudioProjectHircItem item, bool allowUseOfOverrideID, Func<string, uint> hashFunc)
        {
            if (item.OverrideId != 0 && allowUseOfOverrideID)
                item.SerializationId = item.OverrideId;
            else
                item.SerializationId = hashFunc(item.Name);
        }
    }

    /*
     	<!--DialogEvents-->
	<DialogEvent Id="battle_vo_order_halt" bnkFile="gamebnk.bnk">
		<MergeTable Source="Pack|System">customSounds/dialogEvents/battle_vo_order_halt_halflings.csv</MergeTable>
		<MergeTable Source="Pack|System">customSounds/dialogEvents/battle_vo_order_halt_super_goblins.csv</MergeTable>
	</DialogEvent>


	<!--Containers-->
	<RandomContainer id="my_container" ForceId="222s2ss">
		<Instance chance="5000" child="CustomAudioFile_ID"/>
	</RandomContainer>
     */
}


