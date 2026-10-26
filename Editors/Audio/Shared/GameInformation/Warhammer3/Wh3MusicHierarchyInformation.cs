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
        /// The Music Switch containers whose decision trees a State Group's States get a branch in,
        /// keyed by State Group name. These are vanilla hirc ids, so a merge has to re-emit the whole
        /// container the way the Dialogue Event merge re-emits a whole dialogue event. A music node
        /// has one parent, so each container gets a hierarchy of its own over the same audio.
        ///
        /// Every Music Switch container in the game was enumerated, and the multi argument ones
        /// dumped in full. Of the six State Groups the music events target, two can be served:
        ///
        ///   WH3_Campaign_Subcultures                     698158058, subculture x 781561792 -> listed
        ///   Battle_Music_WH3_Culture                     26264058,  result x culture     -> listed
        ///   WH3_Campaign_Subcultures                     145953291, subculture x resolution -> listed
        ///   Battle_Music_WH3_Culture                     67383790,  six arguments deep   -> listed
        ///   WH3_Campaign_Music_AMS_Fragments_Faction     read elsewhere, see below
        ///   WH3_AMS_Pulse_Percussion_Options             read elsewhere, see below
        ///   WH3_AMS_Pulse_Pitched_Orchestral_Options     read elsewhere, see below
        ///   WH3_AMS_Pulse_Pitched_Ethnic_Options         read elsewhere, see below
        ///
        /// Measured on the October 2026 banks: 698158058 is two deep, its second level
        /// (781561792, 5 keys) defaulted in every block, and the merge fills that level in from the
        /// default like any other level it does not branch on.
        ///
        /// 67383790 is the music heard during a battle. Its culture level is the third of six; three
        /// of its six battle phases list all 17 vanilla cultures and offer no default, so a culture
        /// missing from them is silence rather than a fallback - which is what Araby got in game in
        /// October 2026 with only 26264058 merged, prebattle playing and the battle silent. Vanilla's
        /// branches below the culture level are stem sets per musical key, segment and mix; the mod's
        /// branch takes the default at each of those levels instead, so the same music plays whatever
        /// they are. See MusicSwitchContainerMergeService for which blocks get the branch.
        ///
        /// 145953291 is the campaign victory and defeat resolution screens, culture > faction > won or
        /// lost. Vanilla gives each culture its own tracks there, so a culture's tree reaches it too:
        /// its root plays there unless the tree gives the resolution its own branch. The campaign
        /// script ignores every music marker while a resolution screen is up ("we're in campaign
        /// resolution state, will do nothing with markers"), so the segments need no cues of their own.
        ///
        /// The four with no container are the adaptive music system. They are not unused and they are
        /// not all the same: what actually reads each one is two different mechanisms, neither of
        /// them a tree.
        ///
        ///   the three Pulse Groups    9 switch tracks each, one per music track that carries pulses.
        ///                             A switch track holds a sub-track per culture with its own
        ///                             source - 16 for percussion, 11 orchestral, 6 ethnic - and
        ///                             picks between them on the State. Adding a culture means adding
        ///                             a sub-track, a source and an association to nine vanilla
        ///                             Music_Tracks, three times over.
        ///
        ///   AMS_Fragments_Faction     one plain Switch container, 418295225 in campaign_music__core,
        ///                             with 17 switches over 16 children. Not in the music hierarchy
        ///                             at all - the ambient fragments are sounds laid over the music.
        ///
        /// Setting a State none of them lists is harmless rather than broken: each falls back to its
        /// own default, which is 'Cathay' for percussion and 'Off' / 'None' for the other three. So a
        /// new culture that skips these still gets Cathay's percussion pulse and no pitched pulses or
        /// ambient fragments, which is a thinner mix rather than a failure.
        /// </summary>
        public static IReadOnlyList<uint> GetMusicSwitchContainerIds(string stateGroupName) => stateGroupName switch
        {
            "WH3_Campaign_Subcultures" => [698158058, CampaignResolutionMusicSwitchContainerId],
            "Battle_Music_WH3_Culture" => [26264058, InBattleMusicSwitchContainerId],
            _ => []
        };

        /// <summary>
        /// The three State Groups read by switch tracks rather than by a decision tree. A State here
        /// selects a sub-track inside vanilla Music Tracks, so serving one means adding a sub-track
        /// to each of those tracks - see
        /// <see cref="Wwise.Generators.AmsPulseTrackMergeService"/>.
        ///
        /// The tracks themselves are deliberately not listed. There are nine per Group and they are
        /// found by asking the repository which switch tracks name the Group, so this survives a
        /// patch adding or moving one in a way a table of ids would not.
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
        /// setting one of its States still works and still fires, but has to select audio that
        /// already exists rather than bring its own.
        ///
        /// Two different mechanisms answer true here, and they produce quite different things: a
        /// Group with a Music Switch container gets a whole music hierarchy of its own, while a pulse
        /// Group gets a sub-track added to vanilla tracks. What they have in common is that a modder
        /// can pick wavs and have them reached.
        /// </summary>
        public static bool CanCarryOwnAudio(string stateGroupName) =>
            GetMusicSwitchContainerIds(stateGroupName).Count != 0
            || IsAmsPulseStateGroup(stateGroupName)
            || GetAmsFragmentContainerId(stateGroupName) != null;

        /// <summary>
        /// The plain Switch container that branches on the ambient fragments State Group. Unlike the
        /// pulses this is a single container rather than a set of tracks, and it is not in the music
        /// hierarchy at all - the fragments are Sounds laid over the music.
        ///
        /// Its children are themselves Switch containers on the musical key, which is why serving
        /// this Group builds a small hierarchy rather than adding a clip. See
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
        /// Vanilla's stems carry update_dynamicstart and update_dynamicend instead, but those bracket
        /// a stretch the script holds every transition back for, which over a whole piece of music
        /// would hold it back for minutes. Vanilla's own whole tracks carry just these two, measured
        /// in October 2026:
        /// the start 0 to 3.5 seconds in, the end 3 to 10 seconds before the exit cue, median 5.5,
        /// and never earlier than 69% of the way through.
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
        /// The musical grid a music transition is scheduled against, in milliseconds.
        ///
        /// This is not decoration. A transition into a node is timed against its grid, so a node
        /// that declares a period of zero gives Wwise no point in time to make the switch on and
        /// simply never starts - the container selects the branch and nothing is heard.
        ///
        /// Every vanilla child of the containers this touches carries a non-zero period. The ones
        /// that override the meter derive it from their tempo (four bars: four times the beats in a
        /// bar, at 60000/tempo per beat, scaled by the beat value); the ones that do not override it
        /// carry Wwise's own default of 1000 instead. Nothing here knows the tempo of the wav a
        /// modder supplies, so it does not override the meter, and takes the default with it.
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
