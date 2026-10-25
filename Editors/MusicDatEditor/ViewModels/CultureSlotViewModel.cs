using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>One row of the Add Culture wizard: a thing the new culture needs wiring for,
    /// named by the Wwise event it ends up posting.
    ///
    /// The choice on each row is between authoring new audio and borrowing an existing
    /// culture's. Borrowing is not a fallback for the lazy - it is what the shipped scripts do
    /// wherever a culture has no bespoke music of its own, which is why Bretonnia, Dwarfs and
    /// Empire all post music_b_faction_empire.</summary>
    public partial class CultureSlotViewModel : ObservableObject
    {
        readonly Action _changed;

        public MusicDatCultureWiring.CultureSlot Slot { get; }

        [ObservableProperty] bool _include = true;
        [ObservableProperty] bool _useExistingAudio;
        [ObservableProperty] string _referenceCulture = "";
        [ObservableProperty] string _eventInfo = "";
        [ObservableProperty] string _preview = "";

        public string Title => Slot.Title;
        public string Description => Slot.Description;
        public string SkipConsequence => $"If left out: {Slot.SkipConsequence}";
        public IReadOnlyList<string> ReferenceCandidates => Slot.ReferenceCandidates;

        /// <summary>Spelled out per row because the count is the surprise: the ambient fragment
        /// chain is repeated once per randomised variant, so one tick can splice in nine arms.</summary>
        public string Scope => Slot.InsertionCount == 1
            ? "Patches 1 place."
            : $"Patches {Slot.InsertionCount} places (the chain is repeated per randomised variant).";

        /// <summary>
        /// Whether this row's event can be given music of its own, or can only select music that
        /// already exists. An event sets a Wwise State, and a State only reaches new audio if the
        /// Music Switch container branching on its State Group can take a branch. Every container in
        /// the game was enumerated to establish this - see
        /// <see cref="Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds"/> - and only the
        /// campaign subculture slot qualifies. The rest still work as wiring; they just cannot bring
        /// their own files, so saying nothing here would let a modder spend an evening picking wavs
        /// that can never be reached.
        /// </summary>
        public bool CanCarryOwnAudio => Wh3MusicHierarchyInformation.CanCarryOwnAudio(StateGroupName);

        /// <summary>The State Group this row's event sets, or null if the name matches no vanilla
        /// pattern. Resolved from a placeholder culture because the name only differs by suffix.</summary>
        string StateGroupName =>
            Wh3MusicEventInformation.TryResolveStateTarget(Slot.EventFor("placeholder"), out var stateGroupName, out _)
                ? stateGroupName
                : null;

        public string AudioSupportNote
        {
            get
            {
                if (!CanCarryOwnAudio)
                    return "This event can only select music that already exists - the Audio Editor cannot give it " +
                           "files of its own, because nothing in the game branches on its State Group in a way a " +
                           "mod can extend. Point it at an existing culture's music instead.";

                // The adaptive music system layers over whatever theme is playing rather than being a
                // piece of music in its own right, so what a modder should supply is quite different -
                // worth saying before they pick a five minute track for it.
                if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(StateGroupName))
                    return "Audio can be added to this event in the Audio Editor. This is an adaptive music pulse - " +
                           "a rhythmic layer played over the campaign theme, not a theme itself - and the game has " +
                           "nine slots for it. Supply one short loop and it is used for all nine, or up to nine to " +
                           "vary it across the piece. Anything longer than a slot is trimmed to fit.";

                if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(StateGroupName) != null)
                    return "Audio can be added to this event in the Audio Editor. These are ambient fragments - short " +
                           "sounds laid over the campaign music rather than music itself. Vanilla varies them by the " +
                           "musical key the score is in; anything added here plays in every key, since only the " +
                           "composer of a piece can meaningfully write a fragment per key.";

                // Worth saying because the decision tree makes it so rather than anyone choosing it:
                // battle music branches on the result above the culture, and vanilla has no default
                // result, so one culture's music is written under all three outcomes alike.
                if (StateGroupName == BattleCultureStateGroup)
                    return "Audio can be added to this event in the Audio Editor. The same music will play " +
                           "whether the battle is won, lost or drawn - vanilla splits those three, but a " +
                           "single event cannot tell them apart.";

                return "Audio can be added to this event in the Audio Editor.";
            }
        }

        const string BattleCultureStateGroup = "Battle_Music_WH3_Culture";

        public CultureSlotViewModel(MusicDatCultureWiring.CultureSlot slot, Action changed)
        {
            Slot = slot;
            _changed = changed;
        }

        /// <summary>What the arm will post once applied, which is the whole point of the row -
        /// for a new culture it is the event that has to exist in the sound bank, and for a
        /// borrowed one it is the vanilla event being reused.</summary>
        public string EventLine(string musicalCulture)
        {
            if (UseExistingAudio)
                return string.IsNullOrWhiteSpace(ReferenceCulture)
                    ? "Choose an existing culture to borrow from."
                    : $"Reuses '{ReferenceCulture}''s audio - no new Wwise event needed.";

            var name = string.IsNullOrWhiteSpace(musicalCulture) ? "<name>" : musicalCulture;
            var eventName = Slot.EventFor(name);
            return Slot.EventNameIsNegotiable
                ? $"Needs a new Wwise event named {eventName}."
                : $"Needs a new Wwise event named exactly {eventName} - the script builds this name " +
                  "itself, so it has to match precisely.";
        }

        partial void OnIncludeChanged(bool value) => _changed();

        partial void OnUseExistingAudioChanged(bool value)
        {
            // Otherwise the dropdown opens empty and looks broken until something is picked -
            // there is always at least one candidate wherever borrowing is even offered.
            if (value && string.IsNullOrEmpty(ReferenceCulture) && ReferenceCandidates.Count > 0)
                ReferenceCulture = ReferenceCandidates[0];
            _changed();
        }

        partial void OnReferenceCultureChanged(string value) => _changed();
    }
}
