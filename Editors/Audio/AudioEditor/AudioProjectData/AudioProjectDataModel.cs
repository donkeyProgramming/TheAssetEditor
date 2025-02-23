using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectData
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
        public string Language { get; set; } // TODO: Need to use this
    }

    public class ActionEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Event;

        // Technically we should make each action contain the SoundContainer / Sound but making multiple actions for an event isn't supported as the user probably doesn't need to.
        public List<Action> Actions { get; set; } 
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
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
        public RandomSequenceContainer SoundContainer { get; set; }
        public Sound Sound { get; set; }
    }

    public class StatePathNode
    {
        public StateGroup StateGroup { get; set; }
        public State State { get; set; }
    }

    public class RandomSequenceContainer : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.RandomSequenceContainer;
        public uint DirectParentID { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<Sound> Sounds { get; set; }
        public string Language { get; set; }
    }

    public class Sound : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; } = AkBkHircType.Sound;
        public uint DirectParentID { get; set; }
        public uint SourceID { get; set; }
        public string WavFileName { get; set; }
        public string WavFilePath { get; set; }
        public string WemFileName { get; set; }
        public string WemFilePath { get; set; }
        public string WemDiskFilePath { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }
        public uint AttenuationID { get; set; }
    }

    public class AudioSettings
    {
        public PlaylistType PlaylistType { get; set; }
        public bool EnableRepetitionInterval { get; set; }
        public uint? RepetitionInterval { get; set; }
        public EndBehaviour? EndBehaviour { get; set; }
        public bool AlwaysResetPlaylist { get; set; }
        public PlaylistMode PlaylistMode { get; set; }
        public LoopingType LoopingType { get; set; }
        public uint? NumberOfLoops { get; set; }
        public TransitionType TransitionType { get; set; }
        public decimal? TransitionDuration { get; set; }
    }
}
