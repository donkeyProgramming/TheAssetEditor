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
            public string BnkName { get; set; }
            public string RootAudioMixer { get; set; } = CompilerConstants.Mixers_Diplomacy;
            public string Language { get; set; } = CompilerConstants.Language_English;
        }

        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = new List<Event>();
    }
}

