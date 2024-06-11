using Audio.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise.Hirc.Shared;

namespace Audio.BnkCompiler
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
        public AkDecisionTree.Node RootNode { get; set; }
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
        public List<uint> Children { get; set; } = new List<uint>();
        public List<uint> ActorMixerChildren { get; set; } = new List<uint>();
        public string DialogueEvent { get; set; }
    }

    public class ProjectSettings
    {
        public int Version { get; set; } = 1;
        public string OutputGame { get; set; } = CompilerConstants.GameWarhammer3;
        public string BnkName { get; set; }
        public string Language { get; internal set; }
        public uint WwiseStartId { get; set; }
    }

    public class CompilerData
    {
        private readonly List<IAudioProjectHircItem> _projectWwiseObjects = new List<IAudioProjectHircItem>();
        public ProjectSettings ProjectSettings { get; set; } = new ProjectSettings();
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Action> Actions { get; set; } = new List<Action>();
        public List<Sound> Sounds { get; set; } = new List<Sound>();
        public List<ActorMixer> ActorMixers { get; set; } = new List<ActorMixer>();
        public List<RandomContainer> RandomContainers { get; set; } = new List<RandomContainer>();
        public List<DialogueEvent> DialogueEvents { get; set; } = new List<DialogueEvent>();
        public List<string> EventsDat { get; set; } = new List<string>();
        public List<string> StatesDat { get; set; } = new List<string>();

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
