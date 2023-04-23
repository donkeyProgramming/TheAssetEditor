using Audio.BnkCompiler;
using Audio.Utility;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public abstract class IAudioProjectHircItem
    {
        public string Id { get; set; }
        public uint OverrideId { get; set; } = 0;

        [JsonIgnore]
        public uint SerializationId { get; set; }
    }

    public class Action : IAudioProjectHircItem
    {
        public string ChildId { get; set; }
        public string Type { get; set; }
    }

    public class Event : IAudioProjectHircItem
    {
        public string AudioBus { get; set; } = null;
        public List<string> Actions { get; set; }
    }

    public class GameSound : IAudioProjectHircItem
    {
        public string Path { get; set; }
    }

    public class ActorMixer : IAudioProjectHircItem
    {
        public string AudioBus { get; set; } = null;
        public List<string> Sounds { get; set; }
        public List<string> ActorMixerChildren { get; set; }
    }

    public class ProjectSettings
    {
        public int Version { get; set; } = 1;
        public string OutputGame { get; set; } = CompilerConstants.Game_Warhammer3;
        public string BnkName { get; set; }
        public string OutputFilePath { get; set; }
        public string WWiserPath { get; set; }

        public bool ConvertResultToXml { get; set; } = false;
        public bool ThrowOnCompileError { get; set; } = false;
        public bool ExportResultToFile { get; set; } = false;
    }

    public class AudioInputProject
    {
        public ProjectSettings ProjectSettings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Action> Actions { get; set; } = new List<Action>();
        public List<GameSound> GameSounds { get; set; } = new List<GameSound>();
        public List<ActorMixer> ActorMixers { get; set; } = new List<ActorMixer>();

        public void ComputeAllWriteIds(bool allowOverrideIdForActions, bool allowOverrideIdForMixers, bool allowOverrideIdForSounds)
        {
            Events.ForEach(x => Process(x, false, WWiseHash.Compute));
            Actions.ForEach(x => Process(x, allowOverrideIdForActions, WWiseHash.Compute));
            ActorMixers.ForEach(x => Process(x, allowOverrideIdForMixers, WWiseHash.Compute30));
            GameSounds.ForEach(x => Process(x, allowOverrideIdForSounds, WWiseHash.Compute30));
        }

        void Process(IAudioProjectHircItem item, bool allowUseOfOverrideID, Func<string, uint> hashFunc)
        {
            if (item.OverrideId != 0 && allowUseOfOverrideID)
                item.SerializationId = item.OverrideId;
            else
                item.SerializationId = hashFunc(item.Id);
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

