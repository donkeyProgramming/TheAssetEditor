using System.Collections.Generic;

namespace Audio.BnkCompiler
{
    public class CompilerInputProject
    {
        public class Event
        {
            public string Name { get; set; }
            public string Sound { get; set; }
        }

        public class ProjectSettings
        {
            public int Version { get; set; } = 1;
            public string ProjectType { get; set; }
            public string OutputGame { get; set; } = CompilerConstants.Game_Warhammer3;
            public string BnkName { get; set; }
            public string RootAudioMixer { get; set; } = CompilerConstants.Mixers_Diplomacy;
            public string Langauge { get; set; } = CompilerConstants.Language_English;
        }

        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = new List<Event>();
    }
}

