using System.Text;

namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// Finds the one place in each music script where a culture is wired up, and adds a new
    /// one by cloning an existing entry.
    ///
    /// Both shipped files decide their culture the same way: a long else-if chain comparing
    /// a game-supplied string variable against a literal key, each arm assigning a "musical
    /// culture" name and returning. In campaign_music.dat that is
    /// <c>subculture_to_musical_culture</c>, matching the database subculture key
    /// (wh2_main_sc_def_dark_elves) and assigning <c>pending_Subculture</c>. In
    /// battle_music.dat it is <c>cues_TrackEnd_post_culture_events</c>, matching the shorter
    /// key the engine puts in <c>Feedback_Highest_BOP_Alliance_Subculture</c> (dark_elves)
    /// and additionally posting a <c>music_b_faction_*</c> Wwise event.
    ///
    /// Nothing downstream of that chain needs touching, which is what makes this a tractable
    /// edit rather than a compiler problem. The campaign script never names a culture's
    /// event: it builds it at runtime as <c>"music_c_subculture_" + pending_Subculture</c>,
    /// at all 37 sites. The battle script does name its events, so a battle entry carries a
    /// sound event string too.
    ///
    /// A new entry is produced by copying an existing arm verbatim and substituting only its
    /// string constants, so whatever else the arm does - posting an event, setting the
    /// legacy WH1/WH2 request flags, calling a logging helper - is preserved without this
    /// code having to model any of it.
    /// </summary>
    public static class MusicDatCultureWiring
    {
        const uint OpJumpIfFalse = 4;
        const uint OpLoadString = 46;
        const uint OpCompareString = 47;
        const uint OpStoreStringVar = 42;
        const uint OpBindArgString = 43;
        const uint OpCallIntrinsic = 2;
        const uint OpScopeRestore = 6;
        const uint OpCall = 1;
        const uint OpLoadStringVar = 41;
        const uint OpConcatString = 53;

        static readonly HashSet<uint> JumpOpcodes = [3, 4, 55];

        /// <summary>One arm of the chain: "if the key matches, this is the musical culture".
        /// The word offsets are relative to <see cref="Start"/> and mark the constant-pool
        /// indices that a clone substitutes.</summary>
        public record CultureCase(
            string MatchKey,
            string? MusicalCulture,
            string? SoundEvent,
            int Start,
            int End,
            bool CanCloneFrom,
            string BlockedReason,
            int MatchKeyWordOffset,
            int MusicalCultureWordOffset,
            int SoundEventWordOffset)
        {
            public int Length => End - Start;
            public string Display => MusicalCulture == null ? MatchKey : $"{MatchKey}  ->  {MusicalCulture}";
        }

        public record CultureChain(
            string FunctionName,
            string MatchedAgainst,
            string TargetVariable,
            bool UsesSoundEvents,
            IReadOnlyList<CultureCase> Cases)
        {
            public IReadOnlyList<CultureCase> CloneableCases => Cases.Where(c => c.CanCloneFrom).ToList();
        }

        /// <summary>One thing a culture needs wiring for, identified by the Wwise event it
        /// ends up posting.
        ///
        /// Grouping by event rather than by function is what makes this explicable: the three
        /// battle chains are three routes to the same <c>music_b_faction_*</c> event and are
        /// one decision for a modder, while the ambient and pulse chains are separate
        /// decisions that happen to live in the same file. It also collapses the randomised
        /// variants - the ambient-fragment chain is repeated nine times, once per PICK_ONE_OF
        /// case, but all nine post the same event and want the same answer.</summary>
        public record CultureSlot(
            string EventPrefix,
            string Title,
            string Description,
            string SkipConsequence,
            bool EventNameIsNegotiable,
            IReadOnlyList<CultureChain> Chains)
        {
            /// <summary>The event this slot will post for a culture named
            /// <paramref name="musicalCulture"/>.</summary>
            public string EventFor(string musicalCulture) => EventPrefix + musicalCulture;

            /// <summary>How many separate places in the file this slot has to be patched -
            /// more than one where the chain sits inside a randomised PICK_ONE_OF variant.</summary>
            public int InsertionCount => Chains.Count;

            public IReadOnlyList<string> FunctionNames => Chains.Select(c => c.FunctionName).Distinct().ToList();

            /// <summary>Every culture that can be borrowed from and will actually resolve in
            /// <em>every</em> chain this slot has, for a picker UI. A slot can span several
            /// chains - the battle theme is decided in three different functions - and they
            /// do not necessarily carry the same set of arms; the smaller results-screen
            /// chain, say, may not have its own arm for a niche mercenary culture that the
            /// main dispatch chain does. Offering only the intersection is what keeps every
            /// name in the list actually usable: borrowing across a whole slot has to
            /// succeed in each of its chains or not at all, so a name that would fail in even
            /// one of them is not offered.</summary>
            public IReadOnlyList<string> ReferenceCandidates =>
                Chains
                    .Select(c => (IEnumerable<string>)c.CloneableCases.Select(cc => cc.MatchKey))
                    .Aggregate((IEnumerable<string>?)null, (acc, keys) => acc == null
                        ? keys.Distinct(StringComparer.OrdinalIgnoreCase)
                        : acc.Intersect(keys, StringComparer.OrdinalIgnoreCase))
                    ?.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? [];
        }

        /// <summary>Locates the culture chain, or null if this file does not contain one.
        ///
        /// Identified by shape rather than by function name, so it works for both files and
        /// survives a rename: the winner is the chain with the most arms that are a plain
        /// "key maps to one culture name" pair. That is what separates it from the other
        /// long comparison chain in campaign_music.dat, check_for_gameplay_triggers, whose
        /// arms each set three different string variables and so are not something this
        /// operation can extend by copying.</summary>
        public static CultureChain? Find(MusicDatFile file) =>
            FindAll(file).OrderByDescending(c => c.CloneableCases.Count).FirstOrDefault();

        /// <summary>Every culture chain in the file, not just the longest.
        ///
        /// Wiring a culture into a music script means more than the one chain that decides a
        /// battle's theme: each shipped file reaches the same decision from several
        /// directions, and a culture missing from any of them falls back to something wrong
        /// or to nothing. battle_music.dat has the theme chain in
        /// cues_TrackEnd_post_culture_events - which both the automatic choice and a bespoke
        /// battle's override reach, through cues_TrackEnd_check_culture_change (earlier
        /// builds gave each a copy of its own) - and a second for the results screen.
        /// campaign_music.dat has the map-theme chain plus one each for the victory
        /// and defeat resolution screens, and two more that pick ambient fragments and
        /// enemy-proximity percussion.
        ///
        /// The last two live inside PICK_ONE_OF case bodies, so their chain is repeated once
        /// per randomised variant - ten and five times respectively. Those repeats are
        /// separate chains here and are grouped back together by <see cref="FindSlots"/>,
        /// since a caller wants to decide about "ambient fragments" once rather than ten
        /// times.</summary>
        public static IReadOnlyList<CultureChain> FindAll(MusicDatFile file) =>
            MusicDatDisassembler.Disassemble(file)
                .Where(f => f.DecodedExactly)
                .SelectMany(f => FindChainsIn(file, f))
                .Where(c => c.CloneableCases.Count >= 2 && IsCultureChain(c))
                .ToList();

        /// <summary>The culture chains grouped into the things a modder actually decides
        /// about, one per Wwise event. See <see cref="CultureSlot"/> for why that grouping.
        ///
        /// A chain's event is worked out from the bytecode, not assumed. If the script builds
        /// the name at runtime by concatenating a prefix onto the chain's own target variable,
        /// that prefix is the event and the name is NOT negotiable - the script never spells
        /// out a per-culture event, so the soundbank has to contain exactly
        /// <c>prefix + culture name</c> or the post silently does nothing. Otherwise each arm
        /// names its event outright, the shared prefix of those literals is the event family,
        /// and the name IS negotiable: an arm can be pointed at any event that exists, which
        /// is how vanilla gives Bretonnia, Dwarfs and Empire all the same battle music.</summary>
        public static IReadOnlyList<CultureSlot> FindSlots(MusicDatFile file) =>
            FindAll(file)
                .Select(chain => (Chain: chain, Event: EventFamilyOf(file, chain)))
                .Where(x => x.Event.Prefix != null)
                .GroupBy(x => x.Event.Prefix!, StringComparer.Ordinal)
                .Select(g => Describe(g.Key, g.First().Event.IsNegotiable, g.Select(x => x.Chain).ToList()))
                .OrderBy(s => SlotOrder(s.EventPrefix))
                .ToList();

        static (string? Prefix, bool IsNegotiable) EventFamilyOf(MusicDatFile file, CultureChain chain)
        {
            // Built at runtime from this chain's own target variable: the prefix is the event,
            // and nothing in the file can rename it.
            var runtime = FindRuntimeEventPrefixes(file, chain).FirstOrDefault();
            if (runtime != null)
                return (runtime, false);

            // Otherwise the arms name their events. The shared prefix is the family - trimmed
            // back to the last separator so that a run of names that happen to share a leading
            // letter (music_b_faction_cathay / _chaos_wh3) does not over-extend it.
            var events = chain.Cases.Select(c => c.SoundEvent).Where(e => e != null).Distinct().ToList();
            if (events.Count < 2)
                return (null, true);

            var prefix = events.Aggregate((string?)null, (acc, e) => acc == null ? e : CommonPrefix(acc, e!))!;
            var cut = prefix.LastIndexOf('_');
            return cut < 0 ? (null, true) : (prefix[..(cut + 1)], true);
        }

        static string CommonPrefix(string a, string b)
        {
            var n = 0;
            while (n < a.Length && n < b.Length && a[n] == b[n])
                n++;
            return a[..n];
        }

        /// <summary>Editorial labelling for the event families both shipped files use - what
        /// the slot means in the game, and what happens if a culture is left out of it. This
        /// is the one part that cannot be read off the bytecode; it comes from tracing the
        /// callers, and an unrecognised family degrades to a plain description rather than a
        /// guess.</summary>
        static CultureSlot Describe(string prefix, bool negotiable, IReadOnlyList<CultureChain> chains) => prefix switch
        {
            "music_b_faction_" => new CultureSlot(prefix,
                "Battle theme",
                "The music for a battle featuring this culture. Chosen automatically from the dominant " +
                "subculture in the fight, forced by name when a bespoke battle script overrides it, and " +
                "used again on the post-battle results screen.",
                "The battle logs \"no culture has been set, this is a major error\" and plays no theme; " +
                "the results screen falls back to chaos music.",
                negotiable, chains),

            "music_c_subculture_" => new CultureSlot(prefix,
                "Campaign map theme",
                "The music that plays on the campaign map for this culture, and again on the campaign " +
                "victory and defeat resolution screens.",
                "The campaign music never changes to this culture - it keeps playing whatever was already set.",
                negotiable, chains),

            "music_c_ams_" => new CultureSlot(prefix,
                "Ambient fragments",
                "Short culture-flavoured musical phrases layered over the campaign ambient bed, picked " +
                "from several randomised variants.",
                "Logs \"no fragment culture chosen!\" and this culture contributes no ambient flavour.",
                negotiable, chains),

            "music_c_ams_pulse_perc_" => new CultureSlot(prefix,
                "Enemy pulse - percussion",
                "Percussion that plays when a hostile army of this culture is near you on the campaign " +
                "map. Keyed on the enemy's culture, not yours.",
                "Falls back to the vanilla music_c_ams_pulse_perc_agnostic event, so this one degrades gracefully.",
                negotiable, chains),

            "music_c_ams_pulse_orch_" => new CultureSlot(prefix,
                "Enemy pulse - orchestral",
                "The orchestral layer of the same enemy-proximity pulse.",
                "There is no agnostic fallback for this layer, so it is simply silent.",
                negotiable, chains),

            "music_c_ams_pulse_ethnic_" => new CultureSlot(prefix,
                "Enemy pulse - ethnic",
                "The ethnic-instrument layer of the same enemy-proximity pulse.",
                "There is no agnostic fallback for this layer, so it is simply silent.",
                negotiable, chains),

            _ => new CultureSlot(prefix,
                prefix.TrimEnd('_'),
                $"Posts {prefix}<culture>. Found by shape; this build does not have a description for it.",
                "Unknown - this slot was not present when the descriptions were written.",
                negotiable, chains),
        };

        static int SlotOrder(string prefix) => prefix switch
        {
            "music_b_faction_" => 0,
            "music_c_subculture_" => 1,
            "music_c_ams_" => 2,
            "music_c_ams_pulse_perc_" => 3,
            "music_c_ams_pulse_orch_" => 4,
            "music_c_ams_pulse_ethnic_" => 5,
            _ => 99,
        };

        /// <summary>Separates the culture chains from the other else-if chains built the same
        /// way - the ones dispatching on a musical key ("c", "eb", "gb"), a transition state,
        /// or a gameplay trigger. Extending those is a different operation entirely, so they
        /// are filtered out rather than offered.
        ///
        /// Recognised by what the arms are keyed on, which is unambiguous in both files: a
        /// campaign chain tests full subculture keys, every one of which carries the
        /// database's <c>_sc_</c> segment (wh2_dlc09_sc_tmb_tomb_kings), while a battle chain
        /// tests the short audio state and posts a <c>music_b_faction_*</c> event for it. A
        /// chain has to look like one of those for a majority of its arms, so a stray arm
        /// that happens to fit does not drag in a whole unrelated chain.</summary>
        static bool IsCultureChain(CultureChain chain)
        {
            var subcultureKeyed = chain.Cases.Count(c => c.MatchKey.Contains("_sc_", StringComparison.Ordinal));
            var factionEvented = chain.Cases.Count(c =>
                c.SoundEvent?.StartsWith("music_b_faction_", StringComparison.Ordinal) == true);

            return subcultureKeyed * 2 > chain.Cases.Count || factionEvented * 2 > chain.Cases.Count;
        }

        static CultureChain? FindChainIn(MusicDatFile file, MusicDatDisassembler.Function function) =>
            FindChainsIn(file, function).OrderByDescending(c => c.CloneableCases.Count).FirstOrDefault();

        static List<CultureChain> FindChainsIn(MusicDatFile file, MusicDatDisassembler.Function function)
        {
            var body = function.Instructions;

            // Every "compare a literal, branch on failure" site in the function. Not all of
            // them are chain arms: the battle chain sits inside an outer guard of the same
            // shape, and several arms contain a nested test of their own.
            var candidates = new List<(CultureCase Case, int HeadIndex)>();
            for (var i = 0; i + 2 < body.Count; i++)
            {
                if (body[i].Opcode != OpLoadString || body[i + 1].Opcode != OpCompareString || body[i + 2].Opcode != OpJumpIfFalse)
                    continue;

                var armEnd = (int)body[i + 2].Operands[0];
                var hasScopePrologue = i > 0 && body[i - 1].Opcode == OpScopeRestore;
                var armStart = hasScopePrologue ? body[i - 1].Address : body[i].Address;
                if (armEnd <= armStart || armEnd > function.End)
                    continue;

                candidates.Add((BuildCase(file, body, i, armStart, armEnd, hasScopePrologue), i));
            }

            // A chain is a run of arms that hand off to each other: each arm's false-branch is
            // the first word of the next. Guards and nested tests are not part of any such
            // run, so they fall out without needing to be recognised.
            //
            // Every maximal run is reported, not just the longest, because one function can
            // hold several complete chains - AMM_fragments_update repeats its chain once per
            // randomised variant. A run is maximal when nothing hands off to its own first
            // arm, which is what keeps the same chain from being reported once per arm.
            var byStart = candidates.GroupBy(c => c.Case.Start).ToDictionary(g => g.Key, g => g.First());
            var handedOffTo = candidates.Select(c => c.Case.End).ToHashSet();

            var chains = new List<CultureChain>();
            foreach (var candidate in candidates.Where(c => !handedOffTo.Contains(c.Case.Start)))
            {
                var run = new List<(CultureCase Case, int HeadIndex)> { candidate };
                while (byStart.TryGetValue(run[^1].Case.End, out var next))
                    run.Add(next);

                var cases = run.Select(x => x.Case).ToList();
                if (cases.Count < 2 || cases.All(c => c.MusicalCulture == null && c.SoundEvent == null))
                    continue;

                var withCulture = run.FirstOrDefault(x => x.Case.MusicalCulture != null, run[0]);
                chains.Add(new CultureChain(
                    function.Name,
                    DescribeComparand(file, function, run[0].HeadIndex),
                    TargetVariableName(file, body, withCulture.HeadIndex, withCulture.Case.End),
                    cases.Any(c => c.SoundEvent != null),
                    cases));
            }
            return chains;
        }

        static CultureCase BuildCase(MusicDatFile file, List<MusicDatDisassembler.Instruction> body,
            int headIndex, int armStart, int armEnd, bool hasScopePrologue)
        {
            var matchKey = Constant(file, body[headIndex].Operands[0]);
            var matchKeyWordOffset = body[headIndex].Address + 1 - armStart;

            string? musicalCulture = null;
            var musicalCultureWordOffset = -1;
            var musicalCultureCount = 0;
            string? soundEvent = null;
            var soundEventWordOffset = -1;

            for (var j = headIndex + 3; j < body.Count && body[j].Address < armEnd; j++)
            {
                // LOAD_STRING immediately feeding a string-variable assignment.
                if (body[j].Opcode == OpStoreStringVar && j > 0 && body[j - 1].Opcode == OpLoadString)
                {
                    musicalCultureCount++;
                    musicalCulture = Constant(file, body[j - 1].Operands[0]);
                    musicalCultureWordOffset = body[j - 1].Address + 1 - armStart;
                }

                // LOAD_STRING -> BIND_ARG_STRING -> Post SoundEvent.
                if (body[j].Opcode == OpCallIntrinsic && j >= 2 &&
                    body[j - 1].Opcode == OpBindArgString && body[j - 2].Opcode == OpLoadString &&
                    IntrinsicName(file, body[j].Operands[0]).Contains("SoundEvent", StringComparison.OrdinalIgnoreCase))
                {
                    soundEvent = Constant(file, body[j - 2].Operands[0]);
                    soundEventWordOffset = body[j - 2].Address + 1 - armStart;
                }
            }

            // An arm carries its culture either by assigning a name to a string variable or by
            // posting a per-culture sound event - AMM_pulses_choose_percussion only does the
            // latter, since it stores the subculture it matched rather than a name of its own.
            var blocked =
                !hasScopePrologue ? "First arm of the chain - it has no scope prologue, so it cannot be copied." :
                musicalCultureCount == 0 && soundEvent == null ? "This arm reuses another arm's body instead of setting anything of its own." :
                musicalCultureCount > 1 ? "This arm sets the culture in more than one branch, so a copy would be ambiguous." :
                "";

            return new CultureCase(matchKey, musicalCulture, soundEvent, armStart, armEnd,
                blocked.Length == 0, blocked, matchKeyWordOffset, musicalCultureWordOffset, soundEventWordOffset);
        }

        static string TargetVariableName(MusicDatFile file, List<MusicDatDisassembler.Instruction> body, int headIndex, int armEnd)
        {
            for (var j = headIndex + 3; j < body.Count && body[j].Address < armEnd; j++)
                if (body[j].Opcode == OpStoreStringVar && body[j].Operands[0] < file.StringVariables.Count)
                    return file.StringVariables[(int)body[j].Operands[0]].Name;
            return "?";
        }

        /// <summary>What the chain compares against, for display only. Either a parameter or
        /// the string variable loaded just before the chain begins.</summary>
        static string DescribeComparand(MusicDatFile file, MusicDatDisassembler.Function function, int headIndex)
        {
            var slot = unchecked((int)function.Instructions[headIndex + 1].Operands[0]);
            if (slot < 0)
            {
                // Frame offsets are sized rather than a flat -1, -2, ... run: a numeric
                // parameter takes four units and a string one, laid out so the last parameter
                // sits closest to the frame pointer. Both culture chains happen to take a
                // single string parameter, where the two models agree, but computing it
                // properly keeps this label right if a chain is ever found in a function with
                // a different signature.
                var offsets = new int[function.ParameterTypes.Count];
                var runningSize = 0;
                for (var i = function.ParameterTypes.Count - 1; i >= 0; i--)
                {
                    runningSize += function.ParameterTypes[i] == 2 ? 1 : 4;
                    offsets[i] = -runningSize;
                }

                var parameter = Array.IndexOf(offsets, slot);
                return parameter >= 0 ? $"arg{parameter + 1} (the function's parameter)" : $"frame{slot}";
            }

            for (var j = headIndex; j >= 0; j--)
                if (function.Instructions[j].Opcode == 41 && function.Instructions[j].Operands[0] < file.StringVariables.Count)
                    return file.StringVariables[(int)function.Instructions[j].Operands[0]].Name;

            return $"slot{slot}";
        }

        /// <summary>What to do about one slot. <see cref="ReferenceMatchKey"/> null means the
        /// new culture gets its own event name and a Wwise event has to be authored for it;
        /// otherwise the named existing culture's arm is copied wholesale, so the new culture
        /// plays that culture's audio and nothing new is needed on the Wwise side. That second
        /// mode is not a workaround - it is what vanilla does to give Bretonnia, Dwarfs and
        /// Empire one shared battle theme.</summary>
        public record SlotPlan(string EventPrefix, bool Include, string? ReferenceMatchKey);

        /// <summary>Wires a culture into every slot the caller asked for, then validates once.
        ///
        /// Each slot can span several chains - the ambient fragment chain is repeated per
        /// randomised variant - so this can splice in twenty-odd arms. They are inserted in
        /// descending address order, which is what keeps it simple: an insertion only shifts
        /// code after itself, so every remaining insertion point is still where it was found.
        ///
        /// Nothing is applied to <paramref name="file"/> unless every requested slot resolves
        /// first, so a plan naming a culture that cannot be copied fails before the file has
        /// been touched rather than half-way through.
        ///
        /// A chain that already has an arm for <paramref name="matchKey"/> is left alone
        /// rather than given a second, unreachable one. This matters for re-running the
        /// wizard once per subculture of a new culture: the battle chains key on the short
        /// Audio State, which is the same for every subculture unless it has its own
        /// override, so the second run's Audio State collides with the first and should
        /// just no-op there while still adding the campaign chains, which key on the
        /// subculture's own unique full key and so never collide.</summary>
        public static IReadOnlyList<string> AddCulture(MusicDatFile file, string matchKey,
            string musicalCulture, IReadOnlyList<SlotPlan> plans)
        {
            var problems = new List<string>();
            var edits = new List<(CultureCase Template, string Culture, string? Event)>();
            var alreadyWired = 0;

            foreach (var slot in FindSlots(file))
            {
                var plan = plans.FirstOrDefault(p => p.EventPrefix == slot.EventPrefix);
                if (plan is not { Include: true })
                    continue;

                foreach (var chain in slot.Chains)
                {
                    if (chain.Cases.Any(c => string.Equals(c.MatchKey, matchKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        alreadyWired++;
                        continue;
                    }

                    var resolved = Resolve(slot, chain, plan.ReferenceMatchKey, musicalCulture);
                    if (resolved.Problem != null)
                        problems.Add($"{slot.Title} ({chain.FunctionName}): {resolved.Problem}");
                    else
                        edits.Add((resolved.Template!, resolved.Culture!, resolved.Event));
                }
            }

            if (problems.Count > 0)
                return problems;
            if (edits.Count == 0)
                return alreadyWired > 0
                    ? [$"'{matchKey}' already has an entry in every requested slot - nothing to add."]
                    : ["Nothing was selected to add."];

            foreach (var edit in edits.OrderByDescending(e => e.Template.Start))
            {
                var words = BuildClonedArm(file, edit.Template, matchKey, edit.Culture, edit.Event);
                MusicDatCodeEditor.InsertCode(file, edit.Template.Start, words);
            }
            return MusicDatCodeEditor.Validate(file);
        }

        /// <summary>Picks the arm to copy for one chain and works out what to substitute into
        /// it. Referencing an existing culture copies that culture's own arm and changes only
        /// the key it matches, so everything else it does - the event it posts, the legacy
        /// WH1/WH2 flags it sets, the shared tail it jumps to - carries over untouched.</summary>
        static (CultureCase? Template, string? Culture, string? Event, string? Problem) Resolve(
            CultureSlot slot, CultureChain chain, string? referenceMatchKey, string musicalCulture)
        {
            if (referenceMatchKey != null)
            {
                var referenced = chain.Cases.FirstOrDefault(c =>
                    string.Equals(c.MatchKey, referenceMatchKey, StringComparison.OrdinalIgnoreCase));

                if (referenced == null)
                    return (null, null, null, $"'{referenceMatchKey}' is not in this chain. Available: " +
                        string.Join(", ", chain.CloneableCases.Take(8).Select(c => c.MatchKey)));

                if (!referenced.CanCloneFrom)
                    return (null, null, null, $"'{referenceMatchKey}' cannot be copied: {referenced.BlockedReason}");

                // Copy it verbatim - only the match key changes, which BuildClonedArm always
                // substitutes - so the new culture behaves exactly like the referenced one.
                return (referenced, referenced.MusicalCulture ?? musicalCulture, referenced.SoundEvent, null);
            }

            // A new culture of its own: copy the last arm that stands on its own, so the new
            // entry lands at the end of the chain where CA appended the most recent culture.
            var template = chain.CloneableCases.LastOrDefault();
            if (template == null)
                return (null, null, null, "No entry in this chain can be copied.");

            return (template, musicalCulture, slot.EventFor(musicalCulture), null);
        }

        static List<uint> BuildClonedArm(MusicDatFile file, CultureCase model,
            string matchKey, string musicalCulture, string? soundEvent)
        {
            var length = model.Length;
            var words = file.Instructions.GetRange(model.Start, length);

            words[model.MatchKeyWordOffset] = (uint)MusicDatCodeEditor.AddStringConstant(file, matchKey);
            if (model.MusicalCultureWordOffset >= 0)
                words[model.MusicalCultureWordOffset] = (uint)MusicDatCodeEditor.AddStringConstant(file, musicalCulture);
            if (model.SoundEventWordOffset >= 0 && !string.IsNullOrWhiteSpace(soundEvent))
                words[model.SoundEventWordOffset] = (uint)MusicDatCodeEditor.AddStringConstant(file, soundEvent);

            // The copy lands at exactly model.Start, so addresses inside the arm are already
            // correct. So is the arm's own fall-through target, model.End: the model shifts
            // down to exactly there, which is what the copy should hand off to. Only what
            // lies beyond the model - the rest of the chain, any function it calls - moves by
            // the length of the copy. Backwards targets (the chain's shared epilogue) are
            // before the splice and stay put.
            foreach (var ins in Instructions(file, model))
            {
                if (!JumpOpcodes.Contains(ins.Opcode) && ins.Opcode != OpCall)
                    continue;

                var wordOffset = ins.Address + 1 - model.Start;
                var target = (int)words[wordOffset];
                if (target > model.End)
                    words[wordOffset] = (uint)(target + length);
            }

            return words;
        }

        static IEnumerable<MusicDatDisassembler.Instruction> Instructions(MusicDatFile file, CultureCase model) =>
            MusicDatDisassembler.Disassemble(file)
                .SelectMany(f => f.Instructions)
                .Where(i => i.Address >= model.Start && i.Address < model.End);

        /// <summary>Renders the arm one slot will insert, by applying just that slot to a copy
        /// of the file and decompiling the result, so the wizard shows the real thing rather
        /// than a description of it. Where a slot patches several randomised variants they are
        /// all the same arm, so only the first is rendered.</summary>
        public static string PreviewSlot(MusicDatFile file, CultureSlot slot, string matchKey,
            string musicalCulture, string? referenceMatchKey)
        {
            var copy = Clone(file);
            var target = FindSlots(copy).FirstOrDefault(s => s.EventPrefix == slot.EventPrefix);
            if (target == null)
                return "(could not re-locate this slot in a copy of the file)";

            var chain = target.Chains[0];
            var resolved = Resolve(target, chain, referenceMatchKey, musicalCulture);
            if (resolved.Problem != null)
                return resolved.Problem;

            var model = resolved.Template!;
            var problems = AddCulture(copy, matchKey, musicalCulture,
                [new SlotPlan(slot.EventPrefix, true, referenceMatchKey)]);
            if (problems.Count > 0)
                return "This change does not validate:\n  " + string.Join("\n  ", problems);

            var function = MusicDatDisassembler.Disassemble(copy).FirstOrDefault(f => f.Name == chain.FunctionName);
            if (function == null)
                return "(could not re-locate the function after the edit)";

            var lines = MusicDatDecompiler.Decompile(copy, function).Lines.ToList();
            var end = model.Start + model.Length;
            var first = lines.FindIndex(l => l.Address >= model.Start && l.Address < end);
            if (first < 0)
                return "(no preview available)";

            // Found by matching braces from the header rather than by address range: a
            // borrowed arm that itself was only ever a "goto the shared body" redirector
            // renders merged with whichever arm actually owns that body (see
            // MergeAdjacentIdenticalIfChains), and that body's real lines carry the
            // addresses of wherever it physically lives in the function - not this arm's
            // own narrow range. An address-bounded search would stop right after the
            // opening brace and show an empty block.
            var last = first;
            if (lines[first].Text.StartsWith("if (", StringComparison.Ordinal) &&
                first + 1 < lines.Count && lines[first + 1].Text == "{")
            {
                var depth = 0;
                for (var i = first + 1; i < lines.Count; i++)
                {
                    if (lines[i].Text == "{") depth++;
                    else if (lines[i].Text == "}") depth--;
                    last = i;
                    if (depth == 0)
                        break;
                }
            }

            var indent = lines[first].Indent;
            var sb = new StringBuilder();
            for (var i = first; i <= last; i++)
                sb.Append(new string(' ', Math.Max(0, lines[i].Indent - indent) * 4)).AppendLine(lines[i].Text);
            return sb.ToString();
        }

        /// <summary>Deep copy via the file's own writer and reader, so a caller can apply an
        /// edit, validate it, and only then adopt the result.</summary>
        public static MusicDatFile Clone(MusicDatFile file) => MusicDatParser.Parse(MusicDatParser.Write(file));

        /// <summary>Event-name prefixes the script glues onto the chain's culture variable at
        /// runtime, found by looking for <c>LOAD_STRING; LOAD_STRVAR(target); CONCAT</c>.
        ///
        /// This is what a new culture needs on the Wwise side: campaign_music.dat never names
        /// a per-culture event, it builds <c>"music_c_subculture_" + pending_Subculture</c>
        /// at each of its 37 posting sites, so adding a culture here only works if an event
        /// of that name exists. Restricted to strings that look like event names, since the
        /// same variable is also concatenated into log messages.</summary>
        public static IReadOnlyList<string> FindRuntimeEventPrefixes(MusicDatFile file, CultureChain chain)
        {
            var target = file.StringVariables.FindIndex(v => v.Name == chain.TargetVariable);
            if (target < 0)
                return [];

            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var body in MusicDatDisassembler.Disassemble(file).Select(f => f.Instructions))
            {
                for (var i = 0; i + 2 < body.Count; i++)
                {
                    if (body[i].Opcode != OpLoadString ||
                        body[i + 1].Opcode != OpLoadStringVar || body[i + 1].Operands[0] != target ||
                        body[i + 2].Opcode != OpConcatString)
                        continue;

                    var text = Constant(file, body[i].Operands[0]);
                    if (text.StartsWith("music", StringComparison.OrdinalIgnoreCase))
                        found.Add(text);
                }
            }
            return found.ToList();
        }

        static string Constant(MusicDatFile file, uint index) =>
            index < file.StringConstants.Count ? file.StringConstants[(int)index] : $"string{index}";

        static string IntrinsicName(MusicDatFile file, uint index) =>
            index < file.Intrinsics.Count ? file.Intrinsics[(int)index].Name : "";
    }
}
