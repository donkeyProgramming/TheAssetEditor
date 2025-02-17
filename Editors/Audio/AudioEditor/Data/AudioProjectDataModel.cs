using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{
    public class AudioProjectDataModel
    {
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; }
        public List<StateGroup> StateGroups { get; set; }
    }

    public abstract class AudioProjectItem
    {
        public string Name { get; set; }
        public uint ID { get; set; }
    }

    public abstract class AudioProjectHircItem : AudioProjectItem
    {
        public abstract AkBkHircType HircType { get; }
    }

    public partial class SoundBank : AudioProjectItem
    {
        public Wh3SoundBankType SoundBankType { get; set; }
        public Wh3SoundBankSubType SoundBankSubType { get; set; }
        public List<ActionEvent> ActionEvents { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; }
    }

    public class ActionEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Event;
        public List<Action> Actions { get; set; }
        public SoundContainer SoundContainer { get; set; }
        public Sound Sound { get; set; }
    }

    public class Action : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Action;
        public AkActionType ActionType { get; set; } = AkActionType.Play;
        public uint IDExt { get; set; }
    }

    public class DialogueEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Dialogue_Event;
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

    public class SoundContainer : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.SequenceContainer;
        public uint DirectParentID { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<Sound> Sounds { get; set; }
    }

    public class Sound : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Sound;
        public uint DirectParentID { get; set; }
        public uint AttenuationID { get; set; }
        public uint SourceID { get; set; }
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
