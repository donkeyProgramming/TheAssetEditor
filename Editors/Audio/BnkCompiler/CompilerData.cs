using System.Collections.Generic;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.BnkCompiler
{
    public abstract class IAudioProjectHircItem
    {
        public string Name { get; set; }
        public uint Id { get; set; }
    }

    public class Event : IAudioProjectHircItem
    {
        public List<uint> Actions { get; set; }
    }

    public class DialogueEvent : IAudioProjectHircItem
    {
        public AkDecisionTree_V136.Node_V136 RootNode { get; set; }
        public uint NodesCount { get; set; } = 0;

    }

    public class RandomContainer : IAudioProjectHircItem
    {
        public List<uint> Children { get; set; }
        public uint DirectParentId { get; set; } = 0;
    }

    public class Action : IAudioProjectHircItem
    {
        public uint ChildId { get; set; }
        public string Type { get; set; }
    }

    public class Sound : IAudioProjectHircItem
    {
        public string FilePath { get; set; }
        public uint DirectParentId { get; set; } = 0;
        public string DialogueEvent { get; set; }
        public uint Attenuation { get; set; }

    }

    public class ActorMixer : IAudioProjectHircItem
    {
        public uint DirectParentId { get; set; } = 0;
        public List<uint> Children { get; set; } = [];
        public List<uint> ActorMixerChildren { get; set; } = [];
        public string DialogueEvent { get; set; }
    }

    public class ProjectSettings
    {
        public uint Version { get; set; } = 1;
        public string OutputGame { get; set; } = CompilerConstants.GameWarhammer3;
        public string BnkName { get; set; }
        public string Language { get; internal set; }
        public uint WwiseStartId { get; set; }
    }

    public class CompilerData
    {
        private readonly List<IAudioProjectHircItem> _projectWwiseObjects = [];
        public ProjectSettings ProjectSettings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = [];
        public List<Action> Actions { get; set; } = [];
        public List<Sound> Sounds { get; set; } = [];
        public List<ActorMixer> ActorMixers { get; set; } = [];
        public List<RandomContainer> RandomContainers { get; set; } = [];
        public List<DialogueEvent> DialogueEvents { get; set; } = [];
        public List<string> EventsDat { get; set; } = [];
        public List<string> StatesDat { get; set; } = [];

        public void StoreWwiseObjects()
        {
            _projectWwiseObjects.AddRange(Events);
            _projectWwiseObjects.AddRange(Actions);
            _projectWwiseObjects.AddRange(Sounds);
            _projectWwiseObjects.AddRange(ActorMixers);
            _projectWwiseObjects.AddRange(RandomContainers);
            _projectWwiseObjects.AddRange(DialogueEvents);
        }
    }
}
