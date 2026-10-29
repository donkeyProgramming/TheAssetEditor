using System.Collections.Generic;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// The ambient fragment hierarchy one State's <see cref="MusicTree"/> generates, laid out the way
    /// vanilla lays out a culture's - see <see cref="AmsMusicLayout"/>:
    ///
    ///   the vanilla faction Switch container   gains a switch for the State, pointing at
    ///     a Switch on the musical key            one per culture, every key of a group pointing alike
    ///       a Switch on Call or Response         one per distinct pair of things a key plays
    ///         a Sound, or a Random container     per node of the tree that plays there
    ///
    /// Vanilla's "None" key and "None" call/response branch hold a silent placeholder; here they are
    /// left out, which plays nothing the same way. Everything is generated afresh when the tree
    /// changes, so this only records the ids, for the compile to build the switches from and for a
    /// later change to remove.
    /// </summary>
    public class AmsFragment
    {
        public string StateGroupName { get; set; }
        public string StateName { get; set; }

        /// <summary>The vanilla Switch container this culture is added to, which is the one that
        /// branches on <see cref="StateGroupName"/>.</summary>
        public uint FactionSwitchContainerId { get; set; }

        /// <summary>The generated Switch container on the musical key, which the faction container's
        /// new switch points at.</summary>
        public uint KeySwitchContainerId { get; set; }

        /// <summary>Which call/response Switch each key plays; a key not listed plays nothing.</summary>
        public List<AmsFragmentKey> Keys { get; set; } = [];

        public List<AmsFragmentCallResponse> CallResponseSwitches { get; set; } = [];

        /// <summary>Every Sound and Random container generated, all in the SoundBank's own lists.</summary>
        public List<uint> SoundIds { get; set; } = [];
        public List<uint> ContainerIds { get; set; } = [];
    }

    public class AmsFragmentKey
    {
        /// <summary>C, Eb or Gb - see <see cref="AmsMusicLayout.GetKey"/>.</summary>
        public string Key { get; set; }
        public uint CallResponseSwitchId { get; set; }
    }

    /// <summary>A generated Switch on Call or Response, and what each plays: a Sound or a Random
    /// container, or 0 for nothing.</summary>
    public class AmsFragmentCallResponse
    {
        public uint Id { get; set; }
        public uint CallTargetHircId { get; set; }
        public uint ResponseTargetHircId { get; set; }
    }
}
