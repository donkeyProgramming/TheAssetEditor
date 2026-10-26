using System.Text.RegularExpressions;
using Shared.GameFormats.MusicDat;

namespace Test.Audio
{
    // Coverage for the two pieces that let the editor change a music script rather than
    // only read one: the code splice with its address relocation, and the culture chain
    // it is used through.
    public class MusicDatMutationTests
    {
        const string NewSubcultureKey = "mod_sc_test_new_culture";
        const string NewMusicalCulture = "testculture";

        static MusicDatFile Load(string name) => MusicDatFixtures.Load(name);

        static MusicDatCultureWiring.CultureChain RequireChain(MusicDatFile file)
        {
            var chain = MusicDatCultureWiring.Find(file);
            Assert.That(chain, Is.Not.Null, "no culture chain found");
            return chain!;
        }

        /// <summary>The slot holding the file's primary culture chain - the battle theme or the
        /// campaign map theme - so a test can exercise one slot without depending on how many
        /// others the file happens to have.</summary>
        static MusicDatCultureWiring.CultureSlot PrimarySlot(MusicDatFile file, string name)
        {
            var prefix = name.StartsWith("battle", StringComparison.Ordinal) ? "music_b_faction_" : "music_c_subculture_";
            var slot = MusicDatCultureWiring.FindSlots(file).FirstOrDefault(s => s.EventPrefix == prefix);
            Assert.That(slot, Is.Not.Null, $"no {prefix} slot found");
            return slot!;
        }

        static List<MusicDatCultureWiring.SlotPlan> JustThisSlot(MusicDatCultureWiring.CultureSlot slot, string? reference = null) =>
            [new MusicDatCultureWiring.SlotPlan(slot.EventPrefix, true, reference)];

        // Jump labels carry raw addresses, which legitimately move when code is spliced in.
        // Everything else about a function's pseudocode must survive untouched.
        static string Normalise(string pseudocode) => Regex.Replace(pseudocode, @"\bL\d+", "L");

        static Dictionary<string, string> PseudocodeByFunction(MusicDatFile file) =>
            MusicDatDisassembler.Disassemble(file)
                .ToDictionary(f => f.Name, f => Normalise(MusicDatDecompiler.ToText(file, f)));

        [TestCase("campaign_music.dat", "subculture_to_musical_culture", "wh2_main_sc_skv_skaven", "skaven", false)]
        [TestCase("battle_music.dat", "cues_TrackEnd_post_culture_events", "skaven", "skaven", true)]
        public void FindsTheCultureChain(string name, string expectedFunction, string knownKey,
            string knownCulture, bool expectSoundEvents)
        {
            var chain = RequireChain(Load(name));

            Assert.Multiple(() =>
            {
                Assert.That(chain.FunctionName, Is.EqualTo(expectedFunction));
                Assert.That(chain.Cases, Has.Count.GreaterThan(15));
                Assert.That(chain.CloneableCases, Is.Not.Empty);
                Assert.That(chain.UsesSoundEvents, Is.EqualTo(expectSoundEvents));
                Assert.That(chain.Cases.Select(c => c.MatchKey), Does.Contain(knownKey));
                Assert.That(chain.Cases.First(c => c.MatchKey == knownKey).MusicalCulture, Is.EqualTo(knownCulture));
            });
        }

        // The arms of a chain must tile: each one's false branch is the next one's first
        // word. If that ever stops holding, cloning an arm would splice code into a gap.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void ChainArmsAbut(string name)
        {
            var chain = RequireChain(Load(name));

            for (var i = 1; i < chain.Cases.Count; i++)
                Assert.That(chain.Cases[i].Start, Is.EqualTo(chain.Cases[i - 1].End),
                    $"arm {i} ({chain.Cases[i].MatchKey}) does not start where arm {i - 1} ends");
        }

        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void AddCultureProducesAValidFileThatRoundTrips(string name)
        {
            var file = Load(name);

            // Every slot at once - the realistic case, and the one that exercises inserting
            // into the randomised variants and across several chains in one pass.
            var plans = MusicDatCultureWiring.FindSlots(file)
                .Select(s => new MusicDatCultureWiring.SlotPlan(s.EventPrefix, true, null))
                .ToList();

            var problems = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture, plans);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            // Must survive a write/read cycle unchanged - the editor saves through these.
            var reloaded = MusicDatParser.Parse(MusicDatParser.Write(file));
            Assert.That(MusicDatParser.Write(reloaded), Is.EqualTo(MusicDatParser.Write(file)));
            Assert.That(MusicDatCodeEditor.Validate(reloaded), Is.Empty);
        }

        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void AddCultureAppearsInTheChainAndKeepsEveryExistingEntry(string name)
        {
            var file = Load(name);
            var before = RequireChain(file);

            MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture,
                JustThisSlot(PrimarySlot(file, name)));
            var after = RequireChain(file);

            Assert.Multiple(() =>
            {
                Assert.That(after.Cases, Has.Count.EqualTo(before.Cases.Count + 1));
                Assert.That(after.Cases.Select(c => c.MatchKey), Does.Contain(NewSubcultureKey));
                Assert.That(after.Cases.First(c => c.MatchKey == NewSubcultureKey).MusicalCulture,
                    Is.EqualTo(NewMusicalCulture));
                foreach (var original in before.Cases)
                    Assert.That(after.Cases.Select(c => c.MatchKey), Does.Contain(original.MatchKey));
            });
        }

        // The strongest guard on relocation: splicing code in must not change the meaning of
        // any function other than the ones being patched. Comparing decompiled pseudocode
        // catches a mis-relocated jump or call target that a byte-level check would not
        // explain. Run against a single slot so that "every other function" is still most of
        // the file - patching everything would leave little to compare.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void AddCultureLeavesEveryOtherFunctionUnchanged(string name)
        {
            var file = Load(name);
            var slot = PrimarySlot(file, name);
            var patched = slot.FunctionNames.ToHashSet();
            var before = PseudocodeByFunction(file);

            MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture, JustThisSlot(slot));
            var after = PseudocodeByFunction(file);

            Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
            Assert.That(patched, Is.Not.Empty);
            foreach (var (function, text) in before.Where(x => !patched.Contains(x.Key)))
                Assert.That(after[function], Is.EqualTo(text), $"{function} changed");
        }

        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void AddingManyCulturesStaysValid(string name)
        {
            var file = Load(name);

            for (var i = 0; i < 5; i++)
            {
                var problems = MusicDatCultureWiring.AddCulture(file, $"{NewSubcultureKey}_{i}",
                    $"{NewMusicalCulture}{i}", JustThisSlot(PrimarySlot(file, name)));
                Assert.That(problems, Is.Empty, $"iteration {i}: {string.Join("\n", problems)}");
            }

            Assert.That(RequireChain(file).Cases.Count(c => c.MatchKey.StartsWith(NewSubcultureKey)), Is.EqualTo(5));
        }

        // Patching everything is the default the wizard offers, and the case most likely to go
        // wrong: a slot can span several chains - the ambient fragment chain is repeated once per
        // randomised PICK_ONE_OF variant - so one pass splices in many arms whose addresses all
        // shift each other. Every chain of every slot has to come out with the new key in it.
        // How many there are is pinned in MusicDatTests_Explore, not here.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void AddingToEverySlotPatchesEveryChainOfEach(string name)
        {
            var file = Load(name);
            var before = MusicDatCultureWiring.FindSlots(file);
            Assert.That(before.Any(s => s.InsertionCount > 1), Is.True, "needs a slot spanning several chains to be meaningful");

            var problems = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture,
                before.Select(s => new MusicDatCultureWiring.SlotPlan(s.EventPrefix, true, null)).ToList());
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            var after = MusicDatCultureWiring.FindSlots(MusicDatParser.Parse(MusicDatParser.Write(file)));

            Assert.Multiple(() =>
            {
                Assert.That(after.Select(s => s.EventPrefix), Is.EqualTo(before.Select(s => s.EventPrefix)),
                    "a slot went missing after the edit");

                foreach (var slot in after)
                {
                    var patched = slot.Chains.Count(c => c.Cases.Any(x => x.MatchKey == NewSubcultureKey));
                    Assert.That(patched, Is.EqualTo(slot.InsertionCount),
                        $"{slot.Title}: only {patched} of {slot.InsertionCount} chains got the new culture");
                }
            });
        }

        // Referencing an existing culture copies that culture's arm and changes only the key,
        // so the new culture plays the referenced culture's audio and needs nothing authored
        // in Wwise - which is what vanilla does to share one battle theme between Bretonnia,
        // Dwarfs and Empire.
        [TestCase("battle_music.dat", "skaven", "skaven")]
        [TestCase("campaign_music.dat", "wh2_main_sc_skv_skaven", "skaven")]
        public void ReferencingAnExistingCultureReusesItsAudio(string name, string reference, string expectedCulture)
        {
            var file = Load(name);

            var problems = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture,
                JustThisSlot(PrimarySlot(file, name), reference));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            var added = RequireChain(file).Cases.First(c => c.MatchKey == NewSubcultureKey);
            Assert.Multiple(() =>
            {
                // The referenced culture's own name and event carry over, not the new name.
                Assert.That(added.MusicalCulture, Is.EqualTo(expectedCulture));
                Assert.That(added.MusicalCulture, Is.Not.EqualTo(NewMusicalCulture));
                Assert.That(MusicDatCodeEditor.Validate(file), Is.Empty);
            });
        }

        // Bretonnia's own arm in vanilla is just "goto the shared body", so the preview of
        // borrowing it has to show Empire's body, which lives at a different address than the
        // new arm. Bounding the preview by address shows an empty block here.
        [Test]
        public void PreviewOfABorrowedRedirectorArmShowsTheSharedBody()
        {
            var file = Load("battle_music.dat");
            var slot = PrimarySlot(file, "battle_music.dat");

            var preview = MusicDatCultureWiring.PreviewSlot(file, slot, NewSubcultureKey, NewMusicalCulture, "bretonnia");

            Assert.Multiple(() =>
            {
                Assert.That(preview, Does.Contain("PostSoundEvent"), "the shared body must not be cut off after the opening brace");
                Assert.That(preview, Does.Contain("}"), "the block must close");
            });
        }

        // A slot can have several chains, and they do not necessarily carry the same arms -
        // the results-screen chain is shorter than the main dispatch chain. Only cultures
        // resolvable in every one of the slot's chains may be offered to borrow from, or
        // picking one that a smaller chain lacks would fail there.
        [Test]
        public void ReferenceCandidatesAreUsableInEveryChainOfTheSlot()
        {
            var file = Load("battle_music.dat");
            var slot = PrimarySlot(file, "battle_music.dat");

            Assert.That(slot.Chains, Has.Count.GreaterThan(1), "this test needs a multi-chain slot to be meaningful");

            foreach (var candidate in slot.ReferenceCandidates)
                foreach (var chain in slot.Chains)
                    Assert.That(chain.CloneableCases.Select(c => c.MatchKey), Does.Contain(candidate)
                        .IgnoreCase, $"'{candidate}' is offered but missing from {chain.FunctionName}");
        }

        // A plan naming something that cannot be copied has to fail before anything is
        // written, not part-way through a twenty-arm insertion.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void ReferencingSomethingThatCannotBeCopiedIsRejectedWithoutTouchingTheFile(string name)
        {
            var file = Load(name);
            var original = MusicDatParser.Write(file);

            var problems = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture,
                JustThisSlot(PrimarySlot(file, name), "a culture that is definitely not present"));

            Assert.Multiple(() =>
            {
                Assert.That(problems, Is.Not.Empty);
                Assert.That(MusicDatParser.Write(file), Is.EqualTo(original), "the file must be untouched");
            });
        }

        // Wiring a second subculture of a culture that is already present only needs the
        // campaign chains, which key on the subculture's own unique key: the battle chains
        // key on the shared Audio State, which the wizard would be asked to add again
        // unchanged, and that has to be a no-op rather than a second, unreachable arm.
        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void RunningAgainWithTheSameKeyAddsNothingMore(string name)
        {
            var file = Load(name);
            var slot = PrimarySlot(file, name);

            var first = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture, JustThisSlot(slot));
            Assert.That(first, Is.Empty, string.Join("\n", first));
            var afterFirst = MusicDatParser.Write(file);

            var second = MusicDatCultureWiring.AddCulture(file, NewSubcultureKey, NewMusicalCulture, JustThisSlot(slot));

            Assert.Multiple(() =>
            {
                Assert.That(second, Has.Count.EqualTo(1));
                Assert.That(second[0], Does.Contain(NewSubcultureKey).And.Contain("already has an entry"));
                Assert.That(MusicDatParser.Write(file), Is.EqualTo(afterFirst), "a rerun with the same key must not touch the file");
                Assert.That(RequireChain(file).Cases.Count(c => c.MatchKey == NewSubcultureKey), Is.EqualTo(1),
                    "must not add a second, unreachable arm for the same key");
            });
        }

        [TestCase("battle_music.dat")]
        [TestCase("campaign_music.dat")]
        public void PreviewShowsTheNewEntryWithoutChangingTheFile(string name)
        {
            var file = Load(name);
            var original = MusicDatParser.Write(file);

            var preview = MusicDatCultureWiring.PreviewSlot(file, PrimarySlot(file, name),
                NewSubcultureKey, NewMusicalCulture, null);

            Assert.Multiple(() =>
            {
                Assert.That(preview, Does.Contain(NewSubcultureKey));
                Assert.That(preview, Does.Contain(NewMusicalCulture));
                Assert.That(MusicDatParser.Write(file), Is.EqualTo(original), "preview must not mutate the file");
            });
        }

        // What the wizard tells a modder to author in Wwise. The campaign script builds its
        // per-culture event name at runtime, so a new culture there works only if an event
        // called music_c_subculture_<name> exists. The battle script names its events
        // outright instead, so there is no runtime-built prefix to report.
        [TestCase("campaign_music.dat", new[] { "music_c_subculture_" })]
        [TestCase("battle_music.dat", new string[0])]
        public void FindsTheRuntimeBuiltEventPrefixes(string name, string[] expected)
        {
            var file = Load(name);
            var chain = RequireChain(file);

            Assert.That(MusicDatCultureWiring.FindRuntimeEventPrefixes(file, chain), Is.EquivalentTo(expected));
        }

        [Test]
        public void InsertCodeMovesOnlyTheAddressesPastTheSplicePoint()
        {
            var file = Load("battle_music.dat");
            var chain = RequireChain(file);
            var splice = chain.CloneableCases[^1].Start;

            var before = MusicDatDisassembler.Disassemble(file)
                .SelectMany(f => f.Instructions)
                .Where(i => i.Opcode is 3 or 4 or 55)
                .ToDictionary(i => i.Address, i => (int)i.Operands[0]);

            const int shift = 4;
            MusicDatCodeEditor.InsertCode(file, splice, [0, 0, 0, 0]);

            var after = MusicDatDisassembler.Disassemble(file)
                .SelectMany(f => f.Instructions)
                .Where(i => i.Opcode is 3 or 4 or 55)
                .ToDictionary(i => i.Address, i => (int)i.Operands[0]);

            foreach (var (address, target) in before)
            {
                var newAddress = address >= splice ? address + shift : address;
                var expected = target > splice ? target + shift : target;
                Assert.That(after.TryGetValue(newAddress, out var actual), Is.True, $"lost the jump at {address}");
                Assert.That(actual, Is.EqualTo(expected), $"jump at {address} relocated wrongly");
            }
        }

        [Test]
        public void InsertCodeRejectsAnAddressThatIsNotAnInstructionBoundary()
        {
            var file = Load("battle_music.dat");
            var multiWord = MusicDatDisassembler.Disassemble(file)
                .SelectMany(f => f.Instructions)
                .First(i => i.Operands.Length != 0);

            Assert.Throws<ArgumentException>(() => MusicDatCodeEditor.InsertCode(file, multiWord.Address + 1, [0]));
        }

        [Test]
        public void AddStringConstantReusesAnExistingEntry()
        {
            var file = Load("battle_music.dat");
            var count = file.StringConstants.Count;
            var existing = file.StringConstants[^1];

            var reused = MusicDatCodeEditor.AddStringConstant(file, existing);
            var added = MusicDatCodeEditor.AddStringConstant(file, "a string that is definitely not present");

            Assert.Multiple(() =>
            {
                // The first entry holding it, in case the table has duplicates.
                Assert.That(reused, Is.EqualTo(file.StringConstants.IndexOf(existing)));
                Assert.That(added, Is.EqualTo(count));
                Assert.That(file.StringConstants, Has.Count.EqualTo(count + 1));
            });
        }
    }
}
