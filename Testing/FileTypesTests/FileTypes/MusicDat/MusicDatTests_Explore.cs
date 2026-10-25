using Shared.GameFormats.MusicDat;
using Test.TestingUtility.TestUtility;

namespace FileTypesTests.FileTypes.MusicDat
{
    // Coverage for campaign_music.dat / battle_music.dat: the container parse/write and
    // the bytecode disassembly of the embedded music script.
    public class MusicDatTests_Explore
    {
        static readonly string Scratch = Path.Combine(Path.GetTempPath(), "MusicDatDumps");

        static byte[] RawBytes(string name) => PathHelper.GetFileAsBytes($@"Music\{name}");

        static MusicDatFile Load(string name) => MusicDatParser.Parse(RawBytes(name));

        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void ParsesWholeFile(string name) => Load(name);

        // Write() must reproduce the exact original bytes - the guard that lets the
        // editor round-trip a file without silently corrupting anything it doesn't
        // understand yet.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void WriteRoundTripsByteIdentically(string name)
        {
            var original = RawBytes(name);
            var parsed = MusicDatParser.Parse(original);
            var written = MusicDatParser.Write(parsed);

            Assert.That(written, Is.EqualTo(original));
        }

        // The recovered opcode table must tile every function exactly: no leftover
        // words, no unknown opcodes. This is the guard against a regression in
        // MusicDatDisassembler.OperandCount.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void EveryFunctionDisassemblesExactly(string name)
        {
            var funcs = MusicDatDisassembler.Disassemble(Load(name));
            var bad = funcs.Where(f => !f.DecodedExactly).Select(f => f.Name).ToList();

            Assert.That(funcs, Is.Not.Empty);
            Assert.That(bad, Is.Empty, $"functions failed to decode exactly: {string.Join(", ", bad)}");
        }

        // Every jump target must land on an instruction boundary - an independent
        // check that the operand counts are right rather than merely length-consistent.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void EveryJumpTargetLandsOnInstructionBoundary(string name)
        {
            uint[] jumpOpcodes = [3, 4, 55];
            var funcs = MusicDatDisassembler.Disassemble(Load(name));
            var offenders = new List<string>();

            foreach (var f in funcs)
            {
                var boundaries = f.Instructions.Select(i => i.Address).ToHashSet();
                boundaries.Add(f.End);

                foreach (var ins in f.Instructions.Where(i => jumpOpcodes.Contains(i.Opcode)))
                {
                    var target = (int)ins.Operands[0];
                    if (!boundaries.Contains(target))
                        offenders.Add($"{f.Name}@{ins.Address} -> {target}");
                }
            }
            Assert.That(offenders, Is.Empty, $"jump targets not on instruction boundaries: {string.Join(", ", offenders.Take(10))}");
        }

        // Every function in both shipped files now resolves every value back to what
        // produced it - no slot is left showing as a bare slotN. That was not true while
        // FLOAT_TO_STRING and STRING_TO_INT were mis-decoded as zero-operand opcodes: the
        // three functions using them (Update_Load, {b6374121-...}, MM_update_from_music_marker)
        // each had a value the dataflow could not follow, which is the symptom that led back
        // to the wrong arity. Holding the bar at zero is therefore a real guard on the opcode
        // table as much as on the binding model - any regression in either shows up here.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void EveryFunctionFullyDecompiles(string name)
        {
            var file = Load(name);

            var incomplete = MusicDatDisassembler.Disassemble(file)
                .Where(fn => !MusicDatDecompiler.Decompile(file, fn).IsComplete)
                .Select(fn => fn.Name)
                .ToList();

            Assert.That(incomplete, Is.Empty);
        }

        // Wwise_cues (which Music Marker hands every marker to) is the clearest real case of
        // many sibling if-arms sharing one body via a "goto" to a common handler, with arms
        // of their own interleaved among them, breaking the run into several separate
        // consecutive groups rather than one big one - pinning its shape guards that the
        // merge stops at the first differently-bodied arm instead of reaching past it (only
        // strictly consecutive arms merge), that a still-mergeable goto-only arm folds in
        // with the real body its label sits in, and that gotos left over from arms that
        // didn't merge get their short target inlined in place with the label dropped,
        // rather than staying as a bare "goto L;". Its second branch also reaches that
        // target through a second jump ("L26260: goto L25302;"), which a single inlining
        // pass used to leave behind.
        [Test]
        public void AdjacentIfArmsWithIdenticalBodiesMergeIntoOneOrCondition()
        {
            var file = Load("battle_music.dat");
            var function = MusicDatDisassembler.Disassemble(file).First(f => f.Name == "Wwise_cues");

            var text = MusicDatDecompiler.ToText(file, function);

            Assert.Multiple(() =>
            {
                // update_trackstart (the real body's own arm) and update_segment (a goto to
                // it) are consecutive, so they merge.
                Assert.That(text, Does.Contain("if (marker_type == \"update_trackstart\" || marker_type == \"update_segment\")"));

                // "in" sits right after that pair and has its own distinct body, so the run
                // stops there - update_ender and segment, which jump to the same handler,
                // form their own separate merged group instead of joining the first.
                Assert.That(text, Does.Contain("if (marker_type == \"update_ender\" || marker_type == \"segment\")"));
                Assert.That(text, Does.Contain("if (marker_type == \"in\")"));
                Assert.That(text, Does.Not.Contain("update_segment\" || marker_type == \"in\""));

                // update_trackend, update_dynamicstart and update_dynamicend each call a
                // different cues_ handler, so none of them merge with a neighbour.
                Assert.That(text, Does.Contain("if (marker_type == \"update_trackend\")"));
                Assert.That(text, Does.Contain("if (marker_type == \"update_dynamicstart\")"));
                Assert.That(text, Does.Contain("if (marker_type == \"update_dynamicend\")"));
                Assert.That(text, Does.Contain("if (marker_type == \"update_segmentend\" || marker_type == \"segment_end\")"));

                // No goto/label survives: every goto-only arm either merged with the arm
                // holding the label, or had the label's short target code (3 lines)
                // inlined in its place instead.
                Assert.That(text, Does.Not.Contain("goto L"));
                Assert.That(text, Does.Not.Match(@"(?m)^\s*L\d+:\r?$"), "a label survived inlining");

                // Arms with their own distinct body must never be folded into a neighbour's.
                Assert.That(text, Does.Contain("if (marker_type == \"update_transition_allowed\")"));
                Assert.That(text, Does.Contain("transition_allowed = 1;"));
                Assert.That(text, Does.Contain("if (marker_type == \"update_transition_disallowed\")"));
                Assert.That(text, Does.Contain("transition_allowed = 0;"));
                Assert.That(text, Does.Contain("if (marker_type == \"disable_dynamic_states\")"));
                Assert.That(text, Does.Contain("dynamic_states_allowed = 0;"));
                Assert.That(text, Does.Contain("if (marker_type == \"enable_dynamic_states\")"));
                Assert.That(text, Does.Contain("dynamic_states_allowed = 1;"));

                // Every arm that reaches the cues_TrackStart handler still renders it rather
                // than dropping it: trackstart/segment, ender/segment and "out"'s nested
                // check, once in each of the two branches on marker_details.
                Assert.That(text.Split("cues_TrackStart();").Length - 1, Is.EqualTo(6),
                    "expected 3 cues_TrackStart call sites per branch (2 branches x 3)");
            });
        }

        // The ChoicePoints table and the PICK_ONE_OF / JUMP_IF_NOT_CASE pair are what let a
        // multi-way selection render as real "choiceN == k" conditions instead of a chain of
        // gotos with seemingly-dead code after them. Every clause below held on every site in
        // both shipped files when the encoding was worked out (155 in campaign, 1 in battle),
        // which is the whole basis for that rendering - so if a future build breaks one, the
        // switch pseudocode is no longer trustworthy and this should fail loudly rather than
        // let it quietly go back to mis-describing live code as unreachable.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void ChoicePointsTableDescribesEveryPickOneOfSite(string name)
        {
            var file = Load(name);
            var sites = new List<(string Function, int Address, int[] Operands, List<int> Cases, int ValueSlot)>();

            foreach (var fn in MusicDatDisassembler.Disassemble(file))
            {
                var indexOfAddress = fn.Instructions.Select((ins, i) => (ins.Address, i)).ToDictionary(x => x.Address, x => x.i);
                for (var i = 0; i < fn.Instructions.Count; i++)
                {
                    if (fn.Instructions[i].Opcode != 54)
                        continue;

                    var operands = fn.Instructions[i].Operands.Select(x => unchecked((int)x)).ToArray();
                    var valueSlot = operands[2] + 4;

                    // Walk the chain, stopping if it ever tests a different slot - that would
                    // be a nested selection inside a case body, not another case of this one.
                    var cases = new List<int>();
                    for (var j = i + 1; j < fn.Instructions.Count && fn.Instructions[j].Opcode == 55;)
                    {
                        var jump = fn.Instructions[j].Operands.Select(x => unchecked((int)x)).ToArray();
                        if (jump[1] != valueSlot)
                            break;
                        cases.Add(jump[2]);
                        if (!indexOfAddress.TryGetValue(jump[0], out var next))
                            break;
                        j = next;
                    }
                    sites.Add((fn.Name, fn.Instructions[i].Address, operands, cases, valueSlot));
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(sites, Is.Not.Empty);
                Assert.That(file.ChoicePoints, Has.Count.EqualTo(sites.Count),
                    "one ChoicePoints entry per PICK_ONE_OF site");

                for (var i = 0; i < file.ChoicePoints.Count; i++)
                    Assert.That(file.ChoicePoints[i].ChoiceId, Is.EqualTo((uint)i),
                        $"ChoicePoints[{i}].ChoiceId should be its own index");

                foreach (var site in sites)
                {
                    var where = $"{site.Function}@{site.Address}";
                    var choiceId = site.Operands[1];

                    Assert.That(choiceId, Is.InRange(0, file.ChoicePoints.Count - 1), $"{where}: choiceId in range");
                    Assert.That(file.ChoicePoints[choiceId].OptionCount, Is.EqualTo((uint)site.Operands[0]),
                        $"{where}: table option count matches the instruction's");
                    Assert.That(site.Cases, Has.Count.EqualTo(site.Operands[0]),
                        $"{where}: one case per option");
                    Assert.That(site.Cases, Is.EqualTo(Enumerable.Range(2, site.Cases.Count).ToList()),
                        $"{where}: cases numbered contiguously from 2");
                }
            });
        }

        // The payoff from the invariants above: a selection renders as a named value and
        // real equality tests. GMM_change_key is the clearest case in either file - an
        // explicit Key_Override wins if set, otherwise the script picks one of the three
        // musical keys - so it pins both halves of the rendering.
        [Test]
        public void PickOneOfRendersAsNamedChoiceWithRealCaseConditions()
        {
            var file = Load("campaign_music.dat");
            var function = MusicDatDisassembler.Disassemble(file).First(f => f.Name == "GMM_change_key");

            var text = MusicDatDecompiler.ToText(file, function);

            Assert.Multiple(() =>
            {
                // Options render 1..N to match PickOneOf(N), though the bytecode's own
                // constants are 2..N+1 - see TryEmitSwitchCase on why that offset is dropped.
                Assert.That(text, Does.Contain("choice0 = PickOneOf(3);"));
                Assert.That(text, Does.Contain("if (choice0 == 1)"));
                Assert.That(text, Does.Contain("if (choice0 == 2)"));
                Assert.That(text, Does.Contain("if (choice0 == 3)"));
                Assert.That(text, Does.Not.Contain("choice0 == 4"), "option numbers must not exceed the option count");

                // The case bodies are the three keys, and they are reachable code - not a
                // run of gotos followed by text that reads as dead.
                Assert.That(text, Does.Contain("pending_Key = \"c\";"));
                Assert.That(text, Does.Contain("pending_Key = \"eb\";"));
                Assert.That(text, Does.Contain("pending_Key = \"gb\";"));
            });
        }

        // Frame slots are sized, not flat: a numeric value takes four units and a string
        // takes one, and parameters are laid out with the last one closest to the frame
        // pointer. AMM_drone_RTPC_Updater(float, string) pins it from both sides at once,
        // because its two parameters go straight into variables whose own names give away
        // their types - so getting the mapping backwards (which a flat -1, -2, ... stride
        // did) would visibly assign the string parameter to the float variable.
        [Test]
        public void ParametersAreNamedByTheirSizedFrameOffset()
        {
            var file = Load("campaign_music.dat");
            var function = MusicDatDisassembler.Disassemble(file).First(f => f.Name == "AMM_drone_RTPC_Updater");

            var text = MusicDatDecompiler.ToText(file, function);

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.StartWith("function AMM_drone_RTPC_Updater(float arg1, string arg2)"));
                Assert.That(text, Does.Contain("ams_drone_updater_current_rtpc_name = arg2;"));
                Assert.That(text, Does.Contain("ams_drone_updater_current_rtpc_value = arg1;"));
                Assert.That(text, Does.Not.Contain("frame[-1]"), "the string parameter must be recognised as arg2");
                Assert.That(text, Does.Not.Contain("frame[-5]"), "the float parameter must be recognised as arg1");
            });
        }

        // update_timers(float) is the other half of the same rule: a lone numeric parameter
        // sits at -4, not -1, so a flat stride left a delta-time accumulator reading from an
        // anonymous frame cell instead of from its own argument.
        [Test]
        public void LoneNumericParameterIsRecognisedAtItsSizedOffset()
        {
            var file = Load("battle_music.dat");
            var function = MusicDatDisassembler.Disassemble(file).First(f => f.Name == "update_timers");

            Assert.That(MusicDatDecompiler.ToText(file, function),
                Does.Contain("action_battle_length_level_3_timer = arg1 + action_battle_length_level_3_timer;"));
        }

        // Results are written back through slots below the parameters, one per declared
        // ReturnOrLocalTypes entry. The stride used to place them assumes those are all
        // numeric (four units) - true of every entry in both shipped files, asserted here so
        // that a build introducing a string-typed result fails instead of silently sliding
        // every result name onto the wrong slot.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void DeclaredResultSlotsAreNumericAndNamed(string name)
        {
            var file = Load(name);

            Assert.That(file.Functions.SelectMany(f => f.ReturnOrLocalTypes), Is.All.Not.EqualTo(2u),
                "a string-typed result would not be four units wide");

            // update_Check_Battle_End_Status declares two, and its caller reads both - so both ends
            // of the convention should be named rather than showing raw frame offsets.
            if (name != "battle_music.dat")
                return;

            var text = MusicDatDecompiler.ToText(file,
                MusicDatDisassembler.Disassemble(file).First(f => f.Name == "update_Check_Battle_End_Status"));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("ret1 = 1;"));
                Assert.That(text, Does.Contain("ret2 = 1;"));
                Assert.That(text, Does.Not.Contain("frame["));
            });
        }

        // Two slot-binding cases that pull in opposite directions, so a fix for either one
        // alone breaks the other. Worth pinning by their rendered text rather than relying on
        // EveryFunctionFullyDecompiles: the pulse case resolved to a *wrong* expression rather
        // than an unresolved slot, so a completeness check cannot see it at all.
        [Test]
        public void OperandsBindAcrossFreeAndStaleSlots()
        {
            var file = Load("campaign_music.dat");
            var functions = MusicDatDisassembler.Disassemble(file);

            // One stale operand (ams_pulse_type, left over from the previous arm) paired with
            // one freshly loaded operand ("both"). Preferring free operands as a whole group
            // never rebinds the stale one, leaving the previous arm's comparison in its place.
            var pulses = MusicDatDecompiler.ToText(file, functions.First(f => f.Name == "AMM_pulses_choose_percussion"));
            Assert.Multiple(() =>
            {
                Assert.That(pulses, Does.Contain("if (ams_pulse_type == \"both\")"));
                Assert.That(pulses, Does.Not.Match(@"== ""[^""]*"" == ""both"""),
                    "a stale operand rendered as the previous arm's comparison");

                // The mirror case: Feedback_Subculture_Player is loaded once before the chain
                // and re-read by every arm, so by the fall-through it looks stale even though
                // it is only being read. Treating stale as freely writable lets it swallow the
                // log string that belonged to the genuinely free operand beside it.
                var resolution = MusicDatDecompiler.ToText(file,
                    functions.First(f => f.Name == "{601bc71f-4687-4dee-affb-28164d968dd1}"));
                Assert.That(resolution, Does.Contain(
                    "LogMessage(\"campaign resolution: player subculture error, we don't know what this is: \" + Feedback_Subculture_Player);"));
            });
        }

        // The full inventory of places a culture has to be wired in, pinned per file. Adding
        // a culture to only some of them is the failure this guards: the script reaches the
        // same "whose music is this" decision from several directions, and a culture missing
        // from one of them falls back to something wrong or to nothing.
        //
        // Pinned exactly because both directions are bugs. A chain disappearing means the
        // wizard silently stops patching somewhere it used to. A new one appearing means
        // either a real chain was previously being missed, or - more likely - the classifier
        // has started dragging in one of the same-shaped chains that dispatch on musical keys
        // ("c"/"eb"/"gb"), transition states or gameplay triggers, which are a different
        // operation and must not be offered as culture slots.
        [TestCase("battle_music.dat", new[]
        {
            "On Showing Results:1",
            "cues_TrackEnd_post_culture_events:1",
        })]
        [TestCase("campaign_music.dat", new[]
        {
            "AMM_fragments_update:9",
            "AMM_pulses_choose_ethnic:1",
            "AMM_pulses_choose_orchestral:3",
            "AMM_pulses_choose_percussion:5",
            "subculture_to_musical_culture:1",
            "{601bc71f-4687-4dee-affb-28164d968dd1}:1",
            "{df997237-ff3d-4207-9a4a-e7eab4d821ec}:1",
        })]
        public void EveryCultureChainIsFound(string name, string[] expected)
        {
            var file = Load(name);

            // Grouped by function: the ambient-fragment and pulse chains sit inside PICK_ONE_OF
            // case bodies, so each is repeated once per randomised variant and a caller wants
            // to decide about them once rather than once per variant.
            var found = MusicDatCultureWiring.FindAll(file)
                .GroupBy(c => c.FunctionName)
                .Select(g => $"{g.Key}:{g.Count()}")
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            Assert.That(found, Is.EqualTo(expected.OrderBy(x => x, StringComparer.Ordinal).ToList()));
        }

        // The chains above, grouped into the decisions a modder actually makes - one per Wwise
        // event. Pinned as "event prefix : how many places get patched : is the name
        // negotiable", because all three are things the wizard tells the user and all three
        // are wrong in different ways if the grouping drifts.
        //
        // The negotiable flag is the load-bearing one: a false means the script builds the
        // event name at runtime, so the soundbank MUST contain exactly prefix+name and the
        // wizard cannot offer to point the arm somewhere else. Flipping it by accident would
        // have the wizard promise a choice that silently does nothing in game.
        [TestCase("battle_music.dat", new[]
        {
            "music_b_faction_:2:negotiable",
        })]
        [TestCase("campaign_music.dat", new[]
        {
            "music_c_subculture_:3:fixed",
            "music_c_ams_:9:fixed",
            "music_c_ams_pulse_perc_:5:negotiable",
            "music_c_ams_pulse_orch_:3:negotiable",
            "music_c_ams_pulse_ethnic_:1:negotiable",
        })]
        public void CultureChainsGroupIntoSlotsByTheirWwiseEvent(string name, string[] expected)
        {
            var file = Load(name);

            var slots = MusicDatCultureWiring.FindSlots(file)
                .Select(s => $"{s.EventPrefix}:{s.InsertionCount}:{(s.EventNameIsNegotiable ? "negotiable" : "fixed")}")
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(slots, Is.EqualTo(expected));

                // Every chain has to land in exactly one slot - a chain that fell out during
                // grouping would be a place the wizard silently never patches.
                Assert.That(MusicDatCultureWiring.FindSlots(file).Sum(s => s.InsertionCount),
                    Is.EqualTo(MusicDatCultureWiring.FindAll(file).Count), "a chain was dropped while grouping");

                // Each slot must be able to say what it will post and what skipping it costs.
                foreach (var slot in MusicDatCultureWiring.FindSlots(file))
                {
                    Assert.That(slot.EventFor("chaosdwarfs"), Is.EqualTo(slot.EventPrefix + "chaosdwarfs"));
                    Assert.That(slot.Title, Is.Not.Empty);
                    Assert.That(slot.SkipConsequence, Does.Not.Contain("Unknown"), $"{slot.EventPrefix} has no description");
                }
            });
        }

        // A guard against the merge pass silently corrupting brace structure - every
        // decompiled function, not just the one hand-picked case above, must still produce
        // matched braces after merging runs.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void MergedPseudocodeStaysBraceBalanced(string name)
        {
            var file = Load(name);
            var unbalanced = new List<string>();

            foreach (var fn in MusicDatDisassembler.Disassemble(file))
            {
                var text = MusicDatDecompiler.ToText(file, fn);
                if (text.Count(c => c == '{') != text.Count(c => c == '}'))
                    unbalanced.Add(fn.Name);
            }

            Assert.That(unbalanced, Is.Empty);
        }

        [TestCase("battle_music.dat", "battle")]
        [TestCase("campaign_music.dat", "campaign")]
        public void WriteDumps(string name, string prefix)
        {
            var parsed = Load(name);
            Directory.CreateDirectory(Scratch);
            File.WriteAllText(Path.Combine(Scratch, $"{prefix}_disasm.txt"), MusicDatDisassembler.ToText(parsed));

            var sb = new System.Text.StringBuilder();
            foreach (var fn in MusicDatDisassembler.Disassemble(parsed))
                sb.AppendLine(MusicDatDecompiler.ToText(parsed, fn)).AppendLine();
            File.WriteAllText(Path.Combine(Scratch, $"{prefix}_pseudo.txt"), sb.ToString());

            TestContext.Out.WriteLine($"wrote dumps to {Scratch}");
        }
    }
}
