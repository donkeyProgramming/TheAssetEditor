namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    /// <summary>
    /// The shape of a vanilla music branch, read out of the shipped banks rather
    /// than inferred.
    ///
    /// A music Action Event only sets a State. What turns that State into audio is a branch in one
    /// of the Music Switch containers, and a mod adds music by merging a branch of its own into
    /// that container's decision tree - the same way the Audio Editor already merges Dialogue Event
    /// decision trees into the vanilla ones.
    ///
    /// The branch vanilla builds for one culture, taken from WH3_Campaign_Subcultures / 'Dwarfs':
    ///
    ///   MusicSwitch 698158058           args=[State WH3_Campaign_Subcultures, 781561792], depth 2
    ///     node key=hash('Dwarfs')
    ///       node key=0 (default)        -> audioNodeId = the Music Random Sequence below
    ///       Music_Random_Sequence       parent = the switch container, one playlist root
    ///         Music_Segment             parent = the random sequence, one child, one track
    ///           3 markers               entry at the segment start, a named cue, exit at the end
    ///           Music_Track             parent = the segment, one source, one clip
    ///             source                Vorbis, streamed, pointing at the mod's wem
    ///             clip                  playAt / trim / duration in milliseconds
    ///
    /// Every segment observed reuses the same three marker ids, so they are fixed cue identifiers
    /// rather than per-segment values.
    /// </summary>
    public static class Wh3MusicHierarchyInformation
    {
        /// <summary>
        /// The vanilla Music Switch containers whose decision trees a State Group's States get a
        /// branch in. A merge re-emits the whole container, and since a music node has one parent,
        /// each container gets a hierarchy of its own over the same audio.
        ///
        ///   698158058   campaign map theme, subculture x a second level that is always defaulted
        ///   145953291   campaign victory/defeat screens, culture > faction > won or lost
        ///   26264058    battle results (prebattle), result x culture, no default at either level
        ///   67383790    in-battle, culture is level 3 of 6; three of its phases have no default,
        ///               so a culture missing there is silence. Levels below culture are stem sets
        ///               per key/segment/mix, which a mod's branch takes the default of.
        ///
        /// The four AMS State Groups have no container here: the pulse Groups are read by switch
        /// tracks (<see cref="IsAmsPulseStateGroup"/>) and the fragments Group by a plain Switch
        /// container (<see cref="GetAmsFragmentContainerId"/>). A State none of them lists falls back
        /// to that Group's default, so a culture that skips them gets a thinner mix, not silence.
        ///
        /// These ids can change in a game update; the build reports a missing one rather than
        /// shipping without it.
        /// </summary>
        public static IReadOnlyList<uint> GetMusicSwitchContainerIds(string stateGroupName) => stateGroupName switch
        {
            "WH3_Campaign_Subcultures" => [698158058, CampaignResolutionMusicSwitchContainerId],
            "Battle_Music_WH3_Culture" => [26264058, InBattleMusicSwitchContainerId],
            _ => []
        };

        /// <summary>
        /// The three State Groups read by switch tracks rather than by a decision tree: a State
        /// selects a sub-track inside vanilla Music Tracks, so serving one means adding a sub-track
        /// to each - see <see cref="Wwise.Generators.AmsPulseTrackMergeService"/>. The tracks are
        /// found by which ones name the Group rather than listed, so a patch moving one is harmless.
        /// </summary>
        public static bool IsAmsPulseStateGroup(string stateGroupName) => stateGroupName switch
        {
            "WH3_AMS_Pulse_Percussion_Options" => true,
            "WH3_AMS_Pulse_Pitched_Orchestral_Options" => true,
            "WH3_AMS_Pulse_Pitched_Ethnic_Options" => true,
            _ => false
        };

        /// <summary>
        /// Whether a mod can give a State Group's States their own audio. False means an Action Event
        /// setting one of its States still fires, but can only select audio that already exists.
        /// </summary>
        public static bool CanCarryOwnAudio(string stateGroupName) =>
            GetMusicSwitchContainerIds(stateGroupName).Count != 0
            || IsAmsPulseStateGroup(stateGroupName)
            || GetAmsFragmentContainerId(stateGroupName) != null;

        /// <summary>
        /// The plain Switch container (in campaign_music__core) that branches on the ambient fragments
        /// State Group. It is outside the music hierarchy - the fragments are Sounds laid over the
        /// music - and its children are Switch containers on the musical key. See
        /// <see cref="Wwise.Generators.AmsFragmentMergeService"/>.
        /// </summary>
        public static uint? GetAmsFragmentContainerId(string stateGroupName) => stateGroupName switch
        {
            "WH3_Campaign_Music_AMS_Fragments_Faction" => 418295225,
            _ => null
        };

        /// <summary>
        /// Cue markers. Every vanilla music segment carries these same three ids: an entry cue at
        /// the segment start, a named cue the campaign scripts sync against, and an exit cue at the
        /// end. Only the positions differ per segment.
        /// </summary>
        public const uint EntryCueMarkerId = 43573010;
        public const uint CustomCueMarkerId = 41514146;
        public const uint ExitCueMarkerId = 1539036744;

        /// <summary>The named cue's name, which is stored null terminated on disk.</summary>
        public const string CustomCueName = "Update_PreBattle\0";

        /// <summary>The campaign victory and defeat screens - see <see cref="GetMusicSwitchContainerIds"/>.</summary>
        public const uint CampaignResolutionMusicSwitchContainerId = 145953291;

        /// <summary>The adaptive container playing during a battle - see <see cref="GetMusicSwitchContainerIds"/>.</summary>
        public const uint InBattleMusicSwitchContainerId = 67383790;

        /// <summary>
        /// The named cues a segment under <see cref="InBattleMusicSwitchContainerId"/> carries instead
        /// of <see cref="CustomCueName"/>, which battle_music.dat does not recognise. A marker id is
        /// the Wwise hash of its name, the same as for the three above.
        ///
        /// battle_music.dat's Wwise_cues splits a marker name at the colon and runs a handler for the
        /// part before it. update_trackstart is where the script makes a pending track type current
        /// and allows transitions again; update_trackend is where it disallows mid-track transitions,
        /// checks for a culture change and forces a key change. Without them the script never learns
        /// that a track began or ended, so it never notices the strongest culture changing.
        ///
        /// Vanilla's stems carry update_dynamicstart/end instead, but those hold every transition back
        /// for the stretch they bracket, which over a whole piece would be minutes. Vanilla's whole
        /// tracks carry just these two: the start 0 to 3.5 s in, the end 3 to 10 s before the exit cue.
        /// </summary>
        public const uint TrackStartCueMarkerId = 2376622016;
        public const string TrackStartCueName = "Update_TrackStart:\0";
        public const uint TrackEndCueMarkerId = 2301724853;
        public const string TrackEndCueName = "Update_TrackEnd:\0";

        /// <summary>How long before the exit cue the track end cue sits, unless that would put it
        /// earlier than <see cref="TrackEndCueEarliestShare"/> of the way through a short piece.</summary>
        public const double TrackEndCueLeadMs = 5000;
        public const double TrackEndCueEarliestShare = 0.8;

        /// <summary>
        /// The musical grid a transition is scheduled against, in milliseconds. A node with a period
        /// of zero gives Wwise no point to switch on, so it never starts and nothing is heard. Vanilla
        /// nodes that do not override the meter carry Wwise's default of 1000; nothing here knows the
        /// tempo of a modder's wav, so generated nodes do the same.
        /// </summary>
        public const double DefaultGridPeriod = 1000;

        /// <summary>Vorbis. Every vanilla music source uses it, and it is what the compiler encodes
        /// wavs to, so a generated track has to declare the same plugin.</summary>
        public const uint VorbisPluginId = 262145;

        /// <summary>Normal (non-switch, non-sequence) track. Switch tracks are type 3.</summary>
        public const byte NormalTrackType = 0;

        /// <summary>Milliseconds of look ahead, the same on every vanilla music track.</summary>
        public const int TrackLookAheadTime = 100;

        /// <summary>
        /// Music nodes sit under their own parent in the hierarchy rather than under an actor mixer.
        /// Every vanilla segment and track leaves the bus alone; random sequences mostly do too, but
        /// not always - 'Dwarfs' routes to bus 3128400633 while the other subculture branches are
        /// zero. Zero means inherit, which is what a generated branch wants.
        /// </summary>
        public const uint NoOverrideBusId = 0;

        /// <summary>
        /// The transition rule on a Music Random Sequence. Every subculture branch carries exactly
        /// one, and it is byte for byte the same in all of them: any source to any destination
        /// (-1/-1), no fade, synchronised at the exit cue, playing through the pre-entry and
        /// post-exit regions.
        /// </summary>
        public const uint AnyTransitionId = 0xFFFFFFFF;
        public const uint TransitionFadeCurve = 4;
        public const uint TransitionSyncTypeExitCue = 7;

        /// <summary>
        /// The playlist root's type: continuous sequence when there is a single segment, random step
        /// when there is more than one. Vanilla uses exactly this split, pairing the random case
        /// with avoid-repeat and weighting so variations do not play twice in a row.
        /// </summary>
        public const uint PlaylistTypeSequenceContinuous = 0;
        public const uint PlaylistTypeRandomStep = 3;

        /// <summary>Leaf playlist nodes carry no type of their own.</summary>
        public const uint PlaylistTypeNone = 0xFFFFFFFF;

        /// <summary>The weight on every vanilla playlist node, root and leaf alike.</summary>
        public const uint PlaylistDefaultWeight = 50000;

        /// <summary>The weight and probability on every branch of a vanilla music decision tree.
        /// They are the same on all of them, including the default branch.</summary>
        public const ushort BranchWeight = 50;
        public const ushort BranchProbability = 100;
    }
}
