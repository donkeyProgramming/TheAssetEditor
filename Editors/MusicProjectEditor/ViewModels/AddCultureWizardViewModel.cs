using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>Backs the Add Culture wizard: name the new culture once, then decide per slot
    /// whether it gets its own audio or borrows an existing culture's.
    ///
    /// The wizard takes two keys because the two scripts are keyed differently - battle
    /// chains test the short Audio State from the culture table ("chaos_dwarfs") while
    /// campaign chains test the full subculture key ("wh3_dlc23_sc_chd_chaos_dwarfs") - and a
    /// modder should not have to know which file wants which.
    ///
    /// The edit is only ever applied to copies (see <see cref="TryApply"/>), and only copies
    /// that validate are handed back, so a wizard that cannot produce a consistent pair of
    /// files cannot damage the ones being edited.</summary>
    public partial class AddCultureWizardViewModel : ObservableObject
    {
        readonly MusicDatFile _battle;
        readonly MusicDatFile _campaign;

        /// <summary>The edited files, set only after <see cref="TryApply"/> succeeds. Either
        /// can be null when the editor only had one of the pair open.</summary>
        public MusicDatFile? EditedBattle { get; private set; }
        public MusicDatFile? EditedCampaign { get; private set; }

        public ObservableCollection<CultureSlotViewModel> Slots { get; } = [];

        [ObservableProperty] string _audioState = "";
        [ObservableProperty] string _subcultureKey = "";
        [ObservableProperty] string _error = "";

        readonly DispatcherTimer _previewDebounce = new() { Interval = TimeSpan.FromMilliseconds(300) };

        public bool HasBattle => _battle != null;
        public bool HasCampaign => _campaign != null;

        /// <summary>The Audio State as it is actually matched and built into event names -
        /// trimmed and lower-cased. The database has it both ways, so this is normalised here
        /// rather than asking a modder to type it correctly; the box itself stays exactly what
        /// they typed.</summary>
        string NormalizedAudioState => AudioState.Trim().ToLowerInvariant();

        /// <summary>The name used to build event names, always the Audio State verbatim - not
        /// asked separately, since that is what the shipped scripts do.</summary>
        public string MusicalCulture => NormalizedAudioState;

        public string AudioStateHelp =>
            "The culture table's Audio State - what battle_music.dat matches against, and the " +
            "name new events are built from. " +
            $"For example: {Example(_battle, 3)}";

        public string SubcultureKeyHelp =>
            "The full subculture key - what campaign_music.dat matches against. " +
            $"For example: {Example(_campaign, 2)}";

        /// <summary>What a modder needs to know about the Wwise side before authoring
        /// anything, established by checking what the scripts actually call: PostSoundEvent
        /// takes only the event name in both files, and neither ever calls a Switch/State
        /// intrinsic (campaign_music.dat's only other Wwise call, Set RTPC, is a plain numeric
        /// knob set 30 times across the whole file, not a per-culture argument). So an event
        /// cannot itself react to who else is involved - if an existing culture's music
        /// should sound different against this new one, that has to be built as a Switch
        /// Container inside Wwise; nothing here can express or detect that need.</summary>
        public string WwiseNote =>
            "Each slot below only ever posts an event by name - neither script passes anything else " +
            "to Wwise. If an existing culture's music should react to which culture it is being " +
            "played against (a different mix when fighting a particular enemy, say), that has to be " +
            "authored as a Switch Container inside Wwise itself; this wizard has no way to see or " +
            "express that kind of variation.";

        /// <summary>The events the splice will post that do not exist yet, in the order the
        /// rows are shown. A borrowing row contributes nothing: its arm is a clone of an
        /// existing culture's, so it posts that culture's event, which already exists.</summary>
        public IReadOnlyList<string> EventsNeedingAudio =>
            Slots.Where(s => s is { Include: true, UseExistingAudio: false })
                 .Select(s => s.Slot.EventFor(MusicalCulture))
                 .ToList();

        public string AudioProjectHelp =>
            "Adds this culture to the music project with one Music event per row above that brings its own audio, " +
            "built the way vanilla builds them: each event sets a Wwise State naming this culture rather than playing " +
            "a file directly. Nothing is built yet - give the events their wavs in the Music Project tool, then Build " +
            "it to rebuild both music scripts from the game's own copies with every culture in the project. Only rows " +
            "marked as able to carry their own audio can be given files - see the note on each row.";

        /// <summary>What the wizard was given, as the music project keeps it so the scripts can be
        /// rebuilt from vanilla later.</summary>
        public MusicCulture ToMusicCulture() => new()
        {
            AudioState = AudioState.Trim(),
            SubcultureKey = SubcultureKey.Trim(),
            Slots = Slots
                .Where(row => row.Include)
                .Select(row => new MusicCultureSlot
                {
                    EventPrefix = row.Slot.EventPrefix,
                    ReferenceMatchKey = row.UseExistingAudio ? row.ReferenceCulture : null
                })
                .ToList()
        };

        /// <summary>Fills the wizard in from a culture already in the project, so running it again
        /// edits that culture rather than starting from nothing. A slot the culture is not wired
        /// into is left unticked.</summary>
        public void LoadFrom(MusicCulture musicCulture)
        {
            AudioState = musicCulture.AudioState ?? "";
            SubcultureKey = musicCulture.SubcultureKey ?? "";

            foreach (var row in Slots)
            {
                var slot = musicCulture.Slots.FirstOrDefault(slot => slot.EventPrefix == row.Slot.EventPrefix);
                row.Include = slot != null;

                // The culture first, so ticking the box does not pick a default over it.
                row.ReferenceCulture = slot?.ReferenceMatchKey ?? "";
                row.UseExistingAudio = slot?.ReferenceMatchKey != null;
            }
        }

        public string MissingFileWarning =>
            _battle == null ? "battle_music.dat could not be found in this pack, so only the campaign wiring will be added." :
            _campaign == null ? "campaign_music.dat could not be found in this pack, so only the battle wiring will be added." :
            "";

        public AddCultureWizardViewModel(MusicDatFile? battle, MusicDatFile? campaign)
        {
            _battle = battle!;
            _campaign = campaign!;

            foreach (var slot in AllSlots())
                Slots.Add(new CultureSlotViewModel(slot, RefreshPreviews));

            _previewDebounce.Tick += (_, _) =>
            {
                _previewDebounce.Stop();
                RecomputePreviews();
            };
        }

        IEnumerable<MusicDatCultureWiring.CultureSlot> AllSlots()
        {
            if (_battle != null)
                foreach (var slot in MusicDatCultureWiring.FindSlots(_battle))
                    yield return slot;
            if (_campaign != null)
                foreach (var slot in MusicDatCultureWiring.FindSlots(_campaign))
                    yield return slot;
        }

        static string Example(MusicDatFile? file, int take) =>
            file == null
                ? "(that file is not loaded)"
                : string.Join(", ", MusicDatCultureWiring.FindSlots(file)
                    .SelectMany(s => s.Chains[0].Cases).Select(c => c.MatchKey).Distinct().Take(take));

        /// <summary>Which file a slot belongs to, decided by which one reports it rather than
        /// by name, so this keeps working if a slot moves between the files.</summary>
        MusicDatFile FileFor(MusicDatCultureWiring.CultureSlot slot) =>
            _battle != null && MusicDatCultureWiring.FindSlots(_battle).Any(s => s.EventPrefix == slot.EventPrefix)
                ? _battle
                : _campaign;

        string KeyFor(MusicDatCultureWiring.CultureSlot slot) =>
            ReferenceEquals(FileFor(slot), _battle) ? NormalizedAudioState : SubcultureKey;

        partial void OnAudioStateChanged(string value)
        {
            OnPropertyChanged(nameof(MusicalCulture));
            RefreshPreviews();
        }

        partial void OnSubcultureKeyChanged(string value) => RefreshPreviews();

        // The Audio State is required unconditionally, not just when a battle chain is
        // present: it is also where the musical culture name comes from, which every slot
        // (including campaign-only ones) needs to build its event name.
        public bool CanApply =>
            Slots.Any(s => s.Include) &&
            !string.IsNullOrWhiteSpace(AudioState) &&
            (!HasCampaign || !string.IsNullOrWhiteSpace(SubcultureKey)) &&
            Slots.Where(s => s is { Include: true, UseExistingAudio: true })
                 .All(s => !string.IsNullOrWhiteSpace(s.ReferenceCulture));

        /// <summary>Called on every keystroke and every row toggle. <see cref="CanApply"/> is
        /// cheap and updates immediately so the Add button stays responsive; the per-slot
        /// event line and pseudocode preview are not - each one re-parses, edits and
        /// decompiles a whole copy of a file - so recomputing those is debounced rather than
        /// done inline, or typing a key by hand does that work once per character typed.</summary>
        void RefreshPreviews()
        {
            OnPropertyChanged(nameof(CanApply));
            _previewDebounce.Stop();
            _previewDebounce.Start();
        }

        // Bumped on every recompute, so a slow preview that finishes after a newer one started is
        // dropped instead of overwriting it.
        int _previewVersion;

        /// <summary>The event lines are cheap and set straight away. The pseudocode previews are not -
        /// about two seconds for all six rows - so they are worked out off the UI thread, from the
        /// inputs as they were when this ran. The files themselves are only ever read here.</summary>
        async void RecomputePreviews()
        {
            var version = ++_previewVersion;
            var work = new List<(CultureSlotViewModel Row, Func<string> Preview)>();

            foreach (var row in Slots)
            {
                var key = KeyFor(row.Slot);

                if (!row.Include)
                {
                    row.EventInfo = "";
                    row.Preview = row.SkipConsequence;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(MusicalCulture))
                {
                    row.EventInfo = "";
                    row.Preview = "Fill in the fields above to see what will be inserted.";
                    continue;
                }

                row.EventInfo = row.EventLine(MusicalCulture);

                var file = FileFor(row.Slot);
                var slot = row.Slot;
                var musicalCulture = MusicalCulture;
                var reference = row.UseExistingAudio ? row.ReferenceCulture : null;
                work.Add((row, () => MusicDatCultureWiring.PreviewSlot(file, slot, key, musicalCulture, reference)));
            }

            var previews = await Task.Run(() => work.Select(item => TryPreview(item.Preview)).ToList());
            if (version != _previewVersion)
                return;

            for (var i = 0; i < work.Count; i++)
                work[i].Row.Preview = previews[i];
        }

        // Off the UI thread an exception would take the app down rather than reach a handler, and a
        // preview is only ever informational.
        static string TryPreview(Func<string> preview)
        {
            try
            {
                return preview();
            }
            catch (Exception e)
            {
                return $"The preview could not be built: {e.Message}";
            }
        }

        /// <summary>Applies the edit to copies of both files and keeps them only if every slot
        /// resolves and both results validate. The files passed to the constructor are never
        /// touched, and a failure in one file discards the other too rather than leaving the
        /// pair half-wired.</summary>
        public bool TryApply()
        {
            Error = "";
            try
            {
                var battle = _battle == null ? null : MusicDatCultureWiring.Clone(_battle);
                var campaign = _campaign == null ? null : MusicDatCultureWiring.Clone(_campaign);

                var problems = new List<string>();
                if (battle != null)
                    problems.AddRange(Apply(battle, NormalizedAudioState));
                if (campaign != null)
                    problems.AddRange(Apply(campaign, SubcultureKey));

                if (problems.Count > 0)
                {
                    Error = "The edit did not validate, so nothing was changed:\n  " +
                            string.Join("\n  ", problems.Take(10));
                    return false;
                }

                EditedBattle = battle;
                EditedCampaign = campaign;
                return true;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return false;
            }
        }

        IReadOnlyList<string> Apply(MusicDatFile file, string matchKey)
        {
            var wanted = MusicDatCultureWiring.FindSlots(file)
                .Select(slot => Slots.FirstOrDefault(r => r.Slot.EventPrefix == slot.EventPrefix))
                .Where(row => row is { Include: true })
                .Select(row => new MusicDatCultureWiring.SlotPlan(row!.Slot.EventPrefix, true,
                    row.UseExistingAudio ? row.ReferenceCulture : null))
                .ToList();

            return wanted.Count == 0
                ? []
                : MusicDatCultureWiring.AddCulture(file, matchKey, MusicalCulture, wanted);
        }
    }
}
