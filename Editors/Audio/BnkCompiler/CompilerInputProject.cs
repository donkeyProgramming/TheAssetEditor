using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Editors.Audio.BnkCompiler
{
    public class CompilerInputProject
    {
        public class ProjectSettings
        {
            public string BnkName { get; set; }
            public string Language { get; set; }
            public uint WwiseStartId { get; set; }
        }

        public class ProjectDecisionTree
        {
            public string StatePath { get; set; }
            public List<string> Sounds { get; set; }
        }

        public class ProjectDialogueEvent
        {
            public string DialogueEvent { get; set; }
            public List<ProjectDecisionTree> DecisionTree { get; set; }
        }

        public class ProjectEvent
        {
            public string Event { get; set; }
            public string Mixer { get; set; }
            public List<string> Sounds { get; set; }
        }

        public ProjectSettings Settings { get; set; }
        public List<ProjectDialogueEvent> DialogueEvents { get; set; }
        public List<ProjectEvent> Events { get; set; }
    }
}
