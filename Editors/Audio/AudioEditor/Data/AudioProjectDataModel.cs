using System.Collections.Generic;
using System.Collections.ObjectModel;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{
    public class AudioProjectDataModel
    {
        public string Language { get; set; }
        public ObservableCollection<SoundBank> SoundBanks { get; set; }
        public ObservableCollection<StateGroup> StateGroups { get; set; }
    }

    public abstract class AudioProjectItem
    {
        public string Name { get; set; }
        public uint ID { get; set; }
    }

    public partial class SoundBank : AudioProjectItem
    {
        public Wh3SoundBankType Type { get; set; }
        public Wh3SoundBankSubType SubType { get; set; }
        public ObservableCollection<ActionEvent> ActionEvents { get; set; }
        public ObservableCollection<DialogueEvent> DialogueEvents { get; set; }
    }

    public class ActionEvent : AudioProjectItem
    {
        public List<Action> Actions { get; set; }
        public SoundContainer SoundContainer { get; set; }
        public Sound Sound { get; set; }
    }

    public class Action : AudioProjectItem
    {
        public uint ChildID { get; set; }
        public string Type { get; set; }
    }

    public class DialogueEvent : AudioProjectItem
    {
        public List<StatePath> DecisionTree { get; set; }
    }

    public class StateGroup : AudioProjectItem
    {
        public List<State> States { get; set; }
    }

    public class State : AudioProjectItem { }

    public class StatePath
    {
        public List<StatePathNode> Nodes { get; set; } = [];
        public SoundContainer SoundContainer { get; set; }
        public Sound Sound { get; set; }
    }

    public class StatePathNode
    {
        public StateGroup StateGroup { get; set; }
        public State State { get; set; }
    }

    public class SoundContainer : AudioProjectItem
    {
        public uint DirectParentID { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<Sound> Sounds { get; set; }
    }

    public class Sound : AudioProjectItem
    {
        public uint DirectParentID { get; set; }
        public uint AttenuationID { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    public class AudioSettings
    {
        public decimal? Volume { get; set; }
        public decimal? InitialDelay { get; set; }
        public PlaylistType? PlaylistType { get; set; }
        public PlaylistMode? PlaylistMode { get; set; }
        public bool? EnableRepetitionInterval { get; set; }
        public uint? RepetitionInterval { get; set; }
        public EndBehaviour? EndBehaviour { get; set; }
        public bool? EnableLooping { get; set; }
        public bool? LoopInfinitely { get; set; }
        public uint? NumberOfLoops { get; set; }
        public bool? EnableTransitions { get; set; }
        public TransitionType? Transition { get; set; }
        public decimal? Duration { get; set; }
    }
}
