// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;
using System.Security.Cryptography.Xml;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public abstract class IAudioProjectHircItem
    {
        public string Id { get; set; }
        public uint OverrideId { get; set; }
    }

    public class Action : IAudioProjectHircItem
    {
        public string ChildId { get; set; }
        public string Type { get; set; }
    }

    public class Event : IAudioProjectHircItem
    {
        public string AudioBus { get; set; }
        public List<string> Actions { get; set; }
    }

    public class GameSound : IAudioProjectHircItem
    {
        public static string SourceType_Pack = "PackFile";
        public static string SourceType_System = "FileSystem";

        public string Path { get; set; }
        public string SourceType { get; set; } = SourceType_Pack;
        public bool ConvertToWem { get; set; } = false;
    }

    public class ProjectSettings
    {
        public int Version { get; set; } = 1;
        public string OutputGame { get; set; } = "Warhammer3";
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

