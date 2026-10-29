namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// campaign_music.dat / battle_music.dat - the compiled music script that drives
    /// which Wwise music events play in response to game state.
    ///
    /// This is not a declarative state machine: it is a compiled program. The file
    /// holds the symbol tables (game-state triggers, float variables, string
    /// variables, a string constant pool), a function table, a list of engine
    /// intrinsics the script can call, and one flat instruction stream shared by all
    /// functions. See <see cref="MusicDatDisassembler"/> for the instruction encoding.
    /// </summary>
    public class MusicDatFile
    {
        public uint Version { get; set; }

        /// <summary>Named integer/boolean game-state values, with their initial value.
        /// These are what the script branches on (cutscene_active, endbattle, ...).</summary>
        public List<TriggerEntry> Triggers { get; set; } = [];

        /// <summary>Named float values with their initial value - thresholds, timers,
        /// percentages (dynamic_melee_cooldown_threshold, ...).</summary>
        public List<VariableEntry> Variables { get; set; } = [];

        /// <summary>Named string values with their initial value (current_key = "eb", ...).</summary>
        public List<StringVariableEntry> StringVariables { get; set; } = [];

        /// <summary>String constant pool: log text, faction names, and the Wwise event
        /// names the script posts (music_b_faction_empire, ...).</summary>
        public List<string> StringConstants { get; set; } = [];

        /// <summary>One entry per multi-way selection point in the script, in bytecode
        /// order. <see cref="MusicDatDisassembler"/>'s PICK_ONE_OF opcode names an entry by
        /// its <see cref="ChoicePoint.ChoiceId"/> and the entry states how many options that
        /// selection picks between - verified across every site in both shipped files (155
        /// in campaign, 1 in battle): the table has exactly as many entries as there are
        /// PICK_ONE_OF instructions, ChoiceId runs 0..n-1 in bytecode order, and OptionCount
        /// always equals the number of cases in the JUMP_IF_NOT_CASE chain that follows.</summary>
        public List<ChoicePoint> ChoicePoints { get; set; } = [];

        /// <summary>Script functions. <see cref="FunctionEntry.CodeOffset"/> indexes
        /// <see cref="Instructions"/>.</summary>
        public List<FunctionEntry> Functions { get; set; } = [];

        /// <summary>Engine functions callable from the script (Log Message,
        /// Post SoundEvent, Set RTPC).</summary>
        public List<IntrinsicEntry> Intrinsics { get; set; } = [];

        /// <summary>The bytecode: a flat array of (opcode, operands...) words shared by
        /// every function.</summary>
        public List<uint> Instructions { get; set; } = [];

        public class TriggerEntry
        {
            public string Name { get; set; } = "";
            public uint InitialValue { get; set; }
        }

        public class VariableEntry
        {
            public string Name { get; set; } = "";
            public float InitialValue { get; set; }
        }

        public class StringVariableEntry
        {
            public string Name { get; set; } = "";
            public string InitialValue { get; set; } = "";
        }

        /// <summary>A multi-way selection point. <see cref="ChoiceId"/> is the identity the
        /// bytecode refers to it by (and equals the entry's own index); the engine, not the
        /// script, decides which of <see cref="OptionCount"/> options a given execution
        /// takes - nothing in the script feeds the decision.</summary>
        public class ChoicePoint
        {
            public uint ChoiceId { get; set; }
            public uint OptionCount { get; set; }
        }

        /// <summary>A script function. The type lists use 0=bool, 1=float, 2=string.</summary>
        public class FunctionEntry
        {
            public string Name { get; set; } = "";
            public uint CodeOffset { get; set; }
            public List<uint> ParameterTypes { get; set; } = [];
            public List<uint> ReturnOrLocalTypes { get; set; } = [];
        }

        public class IntrinsicEntry
        {
            public string Name { get; set; } = "";
            public uint ParameterCount { get; set; }
            public uint Unknown { get; set; }
            public List<uint> UnknownArray { get; set; } = [];
        }
    }
}
