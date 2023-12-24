using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Audio.BnkCompiler
{
    public class CompilerInputProject
    {
        public class ProjectSettings
        {
            public string BnkName { get; set; }
            public string Language { get; set; } = CompilerConstants.Language_English;
        }

        public class ProjectContents
        {
            public string Event { get; set; }
            public string Type { get; set; }
            public string SoundContainerType { get; set; }
            public string ActorMixer { get; set; }
            public string AudioBus { get; set; }
            public string AuxBus0 { get; set; }
            public string AuxBus1 { get; set; }
            public string AuxBus2 { get; set; }
            public string AuxBus3 { get; set; }
            public string AuxBus4 { get; set; }
            public string StatePropNum_Priority { get; set; }
            public string UserAuxSendVolume0 { get; set; }
            public string InitialDelay { get; set; }
            public List<string> Sounds { get; set; }
        }

        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<ProjectContents> Project { get; set; } = new List<ProjectContents>();

    }
}

