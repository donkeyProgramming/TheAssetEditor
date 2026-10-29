using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.ByteParsing;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    // A music Action Event only sets a State, so a mod's music is unreachable until a branch for
    // that State exists in the vanilla Music Switch container. These cover the merge that adds it.
    internal class MusicSwitchContainerMergeTests
    {
        const uint SwitchContainerId = 698158058;
        const uint BattleContainerId = 26264058;
        const uint NewRanSeqId = 5000;
        const string SubcultureStateGroup = "WH3_Campaign_Subcultures";
        const string BattleCultureStateGroup = "Battle_Music_WH3_Culture";
        const string BattleResultStateGroup = "Battle_Result_State";

        [Test]
        public void ANewStateIsAddedAsABranchPointingAtItsRandomSequence()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            var branch = merged.AkDecisionTree.DecisionTree.Nodes
                .Single(node => node.Key == WwiseHash.Compute("Araby"));

            Assert.Multiple(() =>
            {
                Assert.That(branch.AudioNodeId, Is.EqualTo(NewRanSeqId));
                Assert.That(branch.Weight, Is.EqualTo(Wh3MusicHierarchyInformation.BranchWeight));
                Assert.That(branch.Probability, Is.EqualTo(Wh3MusicHierarchyInformation.BranchProbability));
            });
        }

        [Test]
        public void TheVanillaBranchesAreKept()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            var keys = merged.AkDecisionTree.DecisionTree.Nodes.Select(node => node.Key).ToList();

            Assert.Multiple(() =>
            {
                // The default branch key 0 matters most: it is what plays when no branch matches, so
                // dropping it would silence every culture the mod does not cover.
                Assert.That(keys, Contains.Item(0u));
                Assert.That(keys, Contains.Item(WwiseHash.Compute("Dwarfs")));
                Assert.That(keys, Has.Count.EqualTo(3));
            });
        }

        [Test]
        public void AStateVanillaAlreadyCoversIsReplacedRatherThanDuplicated()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Dwarfs", NewRanSeqId)]);

            var dwarfBranches = merged.AkDecisionTree.DecisionTree.Nodes
                .Where(node => node.Key == WwiseHash.Compute("Dwarfs"))
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(dwarfBranches, Has.Count.EqualTo(1));
                Assert.That(dwarfBranches[0].AudioNodeId, Is.EqualTo(NewRanSeqId), "the mod's branch takes priority");
            });
        }

        [Test]
        public void TheMergedContainerSurvivesAWriteReadRoundTrip()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            var reloaded = new CAkMusicSwitchCntr_V136();
            reloaded.ReadHirc(new ByteChunk(merged.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(SwitchContainerId));
                Assert.That(reloaded.TreeDepth, Is.EqualTo(1));
                Assert.That(reloaded.Arguments.Single().GroupId, Is.EqualTo(WwiseHash.Compute("WH3_Campaign_Subcultures")));

                // The root plus three branches. The size on disk is recomputed from the tree, so a
                // wrong count here would desynchronise everything after the container in the bank.
                Assert.That(reloaded.AkDecisionTree.Nodes, Has.Count.EqualTo(4));
                Assert.That(reloaded.TreeDataSize, Is.EqualTo(merged.AkDecisionTree.GetSize()));

                var branch = reloaded.AkDecisionTree.DecisionTree.Nodes
                    .Single(node => node.Key == WwiseHash.Compute("Araby"));
                Assert.That(branch.AudioNodeId, Is.EqualTo(NewRanSeqId));
            });
        }

        // A container only resolves a decision tree leaf to a node it also claims as a child. A
        // branch added without the matching child is not an error - the container just falls
        // through to its default, so the new culture silently gets vanilla's music. That is what
        // the first end to end pack did, and what these three pin down.
        [Test]
        public void TheNewBranchIsAlsoDeclaredAsAChild()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            var children = merged.MusicTransNodeParams.MusicNodeParams.Children;

            Assert.Multiple(() =>
            {
                Assert.That(children.ChildIds, Does.Contain(NewRanSeqId));
                Assert.That(children.ChildIds, Does.Contain(1056018414u), "vanilla's child was dropped");
                Assert.That(children.NumChilds, Is.EqualTo((uint)children.ChildIds.Count));
                Assert.That(children.ChildIds, Is.Ordered, "vanilla lists children in ascending id order");
            });
        }

        [Test]
        public void EveryLeafOfAMergedBattleTreeIsDeclaredAsAChild()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            var childIds = merged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds;

            Assert.That(LeavesOf(merged.AkDecisionTree.DecisionTree).Where(id => id != 0).Distinct(),
                Is.SubsetOf(childIds));
        }

        // Vanilla trees also point at nodes their container does not claim - four of the six
        // containers in campaign_music__core.bnk do, by up to 349 nodes. Claiming those as well
        // gives them a second parent, and made a container merged with only itself come back larger.
        [Test]
        public void ANodeVanillaPointsAtButDoesNotClaimStaysUnclaimed()
        {
            const uint UnclaimedNodeId = 777;
            var vanillaContainer = CreateVanillaContainer();
            vanillaContainer.AkDecisionTree.DecisionTree.Nodes.Add(CreateBranch(WwiseHash.Compute("Empire"), UnclaimedNodeId));
            vanillaContainer.AkDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(vanillaContainer.AkDecisionTree.DecisionTree);
            var vanillaChildIds = vanillaContainer.MusicTransNodeParams.MusicNodeParams.Children.ChildIds.ToList();

            var service = new MusicSwitchContainerMergeService();
            var merged = service.MergeBranches(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);
            var selfMerged = service.MergeBranches(vanillaContainer, []);

            Assert.Multiple(() =>
            {
                Assert.That(merged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds, Does.Contain(NewRanSeqId));
                Assert.That(merged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds, Does.Not.Contain(UnclaimedNodeId));
                Assert.That(selfMerged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds, Is.EqualTo(vanillaChildIds));
            });
        }

        [Test]
        public void TheChildListIsNotSharedWithTheVanillaContainer()
        {
            // The vanilla hirc belongs to the audio repository, so a child list added to in place
            // would leak the mod's node into every later read of vanilla.
            var vanillaContainer = CreateVanillaContainer();

            new MusicSwitchContainerMergeService()
                .MergeBranches(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.That(vanillaContainer.MusicTransNodeParams.MusicNodeParams.Children.ChildIds,
                Does.Not.Contain(NewRanSeqId));
        }

        [Test]
        public void MergingDoesNotEditTheVanillaContainer()
        {
            // The vanilla hirc comes from the audio repository and is shared with everything else
            // reading vanilla data, so compiling twice must not merge into an already merged tree.
            var vanillaContainer = CreateVanillaContainer();

            new MusicSwitchContainerMergeService()
                .MergeBranches(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.That(vanillaContainer.AkDecisionTree.DecisionTree.Nodes, Has.Count.EqualTo(2));
        }

        [Test]
        public void ABattleCultureGetsAPathUnderEveryResult()
        {
            // Battle music branches on the result before the culture, and vanilla has no default
            // result - it is lose, win or draw and nothing else. So a culture with one piece of
            // music has to appear under all three, or it plays for one outcome and falls silent for
            // the other two.
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            var resultNodes = merged.AkDecisionTree.DecisionTree.Nodes;

            Assert.Multiple(() =>
            {
                Assert.That(resultNodes, Has.Count.EqualTo(3), "the three vanilla results, and no fourth");

                foreach (var resultNode in resultNodes)
                {
                    var arabyNode = resultNode.Nodes.SingleOrDefault(node => node.Key == WwiseHash.Compute("Araby"));
                    Assert.That(arabyNode, Is.Not.Null, $"no Araby under result {resultNode.Key}");
                    Assert.That(arabyNode.AudioNodeId, Is.EqualTo(NewRanSeqId));
                }
            });
        }

        [Test]
        public void OnlyTheLeafOfABattlePathNamesAudio()
        {
            // An intermediate node carrying an audio id is read as a leaf, which would make the
            // culture level below it unreachable and hand every battle the same music.
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.Multiple(() =>
            {
                foreach (var resultNode in merged.AkDecisionTree.DecisionTree.Nodes)
                    Assert.That(resultNode.AudioNodeId, Is.EqualTo(0u), "a result node is not a leaf");
            });
        }

        [Test]
        public void TheVanillaBattleCulturesSurviveUnderEveryResult()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.Multiple(() =>
            {
                foreach (var resultNode in merged.AkDecisionTree.DecisionTree.Nodes)
                {
                    var keys = resultNode.Nodes.Select(node => node.Key).ToList();
                    Assert.That(keys, Contains.Item(WwiseHash.Compute("Empire")));
                    Assert.That(keys, Contains.Item(WwiseHash.Compute("Cathay")));
                    Assert.That(keys, Has.Count.EqualTo(3), "the two vanilla cultures plus the new one");
                }
            });
        }

        [Test]
        public void AMergedBattleContainerSurvivesAWriteReadRoundTrip()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            var reloaded = new CAkMusicSwitchCntr_V136();
            reloaded.ReadHirc(new ByteChunk(merged.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.TreeDepth, Is.EqualTo(2));

                // Three results, each with three cultures, plus the root. A nested tree is written
                // as a flat list with child offsets, so a miscount here is not a missing branch -
                // it desynchronises every hirc after the container in the bank.
                Assert.That(reloaded.AkDecisionTree.Nodes, Has.Count.EqualTo(1 + 3 + 9));
                Assert.That(reloaded.TreeDataSize, Is.EqualTo(merged.AkDecisionTree.GetSize()));

                foreach (var resultNode in reloaded.AkDecisionTree.DecisionTree.Nodes)
                {
                    var arabyNode = resultNode.Nodes.SingleOrDefault(node => node.Key == WwiseHash.Compute("Araby"));
                    Assert.That(arabyNode?.AudioNodeId, Is.EqualTo(NewRanSeqId));
                }
            });
        }

        [Test]
        public void AStateGroupTheContainerDoesNotBranchOnIsRefused()
        {
            // Setting a State whose Group nothing in this container tests would put the branch at no
            // level at all. Refused rather than written somewhere arbitrary.
            Assert.Throws<NotSupportedException>(() => new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaBattleContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]));
        }

        // Stands in for the vanilla Battle_Music_WH3_Culture container 26264058: result first, then
        // culture, no default at either level - trimmed to two of its sixteen cultures.
        /// <summary>Vanilla declares every node its tree names as a child of the container, and
        /// only children are resolved, so the fixtures have to do the same or the merged child
        /// list has nothing to be wrong about.</summary>
        // The in-battle container 67383790 in miniature: a phase level above the culture level and a
        // musical key level below it. Its "fight" phase lists every culture with no default, which is
        // where a culture missing from it gets silence; its "bespoke" phase plays the same thing for
        // every culture and lists none.
        [Test]
        public void ABattleCultureGoesIntoEveryBlockThatListsCulturesAndNoOther()
        {
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(CreateVanillaAdaptiveContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            var araby = WwiseHash.Compute("Araby");
            var phases = merged.AkDecisionTree.DecisionTree.Nodes.ToDictionary(node => node.Key);
            var fight = phases[WwiseHash.Compute("fight")].Nodes.SingleOrDefault(node => node.Key == araby);

            Assert.Multiple(() =>
            {
                Assert.That(fight, Is.Not.Null, "the phase listing every culture has no branch for the new one");
                Assert.That(phases[WwiseHash.Compute("bespoke")].Nodes.Select(node => node.Key), Is.EqualTo(new[] { 0u }),
                    "the culture-neutral phase gained a culture");
                Assert.That(phases.Keys, Has.Count.EqualTo(2), "a phase was added");

                // Below the culture level the branch takes the default, so it plays whatever key the
                // music is in - vanilla's own branches have no default there to fall back on.
                Assert.That(fight!.Nodes.Select(node => node.Key), Is.EqualTo(new[] { 0u }));
                Assert.That(fight.Nodes.Single().AudioNodeId, Is.EqualTo(NewRanSeqId));
                Assert.That(merged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds, Does.Contain(NewRanSeqId));
            });
        }

        [Test]
        public void TheVanillaPathsOfTheAdaptiveContainerAreUntouched()
        {
            var vanilla = CreateVanillaAdaptiveContainer();
            var merged = new MusicSwitchContainerMergeService()
                .MergeBranches(vanilla, [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            var before = PathsOf(vanilla.AkDecisionTree.DecisionTree);
            var after = PathsOf(merged.AkDecisionTree.DecisionTree);

            Assert.That(before.Where(path => !after.TryGetValue(path.Key, out var audioNodeId) || audioNodeId != path.Value), Is.Empty);
            Assert.That(after.Count - before.Count, Is.EqualTo(1), "the new culture should add exactly one path");
        }

        // --- Music trees: the modder's say over which audio plays below the culture ---

        const int RootNodeId = 0;

        // A tree that is only a root is what every event was before trees: the same audio under
        // every path, exactly where the merge without a tree puts it.
        [Test]
        public void ARootOnlyTreeReachesTheSamePathsAsNoTree()
        {
            var container = CreateVanillaBattleContainer();
            var tree = Tree(BattleCultureStateGroup, "Araby", Node(RootNodeId, audio: 1));

            var treePaths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(container, tree);
            var withTree = new MusicSwitchContainerMergeService().MergeBranches(container,
                [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId, treePaths[RootNodeId])]);
            var withoutTree = new MusicSwitchContainerMergeService().MergeBranches(container,
                [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.That(PathsOf(withTree.AkDecisionTree.DecisionTree), Is.EqualTo(PathsOf(withoutTree.AkDecisionTree.DecisionTree)));
        }

        [Test]
        public void ASplitAboveTheCulturePicksAudioPerBlock()
        {
            var root = Node(RootNodeId, audio: 1, splitBy: BattleResultStateGroup);
            root.Branches.Add(Node(1, audio: 2, value: "win"));

            var paths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(CreateVanillaBattleContainer(), Tree(BattleCultureStateGroup, "Araby", root));

            Assert.Multiple(() =>
            {
                Assert.That(paths[1], Is.EquivalentTo(new[] { Keys("win", "Araby") }));
                Assert.That(paths[RootNodeId], Is.EquivalentTo(new[] { Keys("lose", "Araby"), Keys("draw", "Araby") }));
            });
        }

        // Wwise reads the levels in the container's order and prefers an exact key at each, so a
        // path per tree branch would let a split on the key beat a split on the situation even where
        // the modder put the situation first. Every combination is written out instead.
        [Test]
        public void TheModdersOrderOfSplitsWinsOverTheContainersOrderOfLevels()
        {
            var container = CreateContainerBelowTheCulture();
            var root = Node(RootNodeId, audio: 1, splitBy: "Situation");
            root.Branches.Add(Node(1, audio: 2, value: "winning"));
            var anyOther = Node(2, splitBy: "Key");
            anyOther.Branches.Add(Node(3, audio: 3, value: "c"));
            root.Branches.Add(anyOther);

            var paths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(container, Tree(BattleCultureStateGroup, "Araby", root));
            var merged = new MusicSwitchContainerMergeService().MergeBranches(container,
                [.. paths.Select(entry => new MusicBranch(BattleCultureStateGroup, "Araby", 9000 + (uint)entry.Key, entry.Value))]);

            Assert.Multiple(() =>
            {
                Assert.That(Lookup(merged, "Araby", "c", "winning"), Is.EqualTo(9001), "winning is split on first, so it wins in the key of C too");
                Assert.That(Lookup(merged, "Araby", "eb", "winning"), Is.EqualTo(9001));
                Assert.That(Lookup(merged, "Araby", "c", "losing"), Is.EqualTo(9003));
                Assert.That(Lookup(merged, "Araby", "eb", "losing"), Is.EqualTo(9000));
                Assert.That(paths.ContainsKey(2), Is.False, "a node without audio plays its parent's, so nothing reaches it by itself");
            });
        }

        [Test]
        public void ASplitOnThePartOfTheGameKeepsEachContainersAudioApart()
        {
            var root = Node(RootNodeId, audio: 1, splitBy: Wh3MusicTreeLevelInformation.ContainerLevel);
            root.Branches.Add(Node(1, audio: 2, value: BattleContainerId.ToString()));
            var tree = Tree(BattleCultureStateGroup, "Araby", root);

            var battlePaths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(CreateVanillaBattleContainer(), tree);
            var adaptivePaths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(CreateVanillaAdaptiveContainer(), tree);

            Assert.Multiple(() =>
            {
                Assert.That(battlePaths.Keys, Is.EquivalentTo(new[] { 1 }));
                Assert.That(adaptivePaths.Keys, Is.EquivalentTo(new[] { RootNodeId }));
            });
        }

        // The in-battle container has no battle result, so a split on it there is passed over and
        // the node above it plays.
        [Test]
        public void ASplitOnALevelTheContainerDoesNotHaveIsIgnoredThere()
        {
            var root = Node(RootNodeId, audio: 1, splitBy: BattleResultStateGroup);
            root.Branches.Add(Node(1, audio: 2, value: "win"));
            root.Branches.Add(Node(2, audio: 3));

            var paths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(CreateVanillaAdaptiveContainer(), Tree(BattleCultureStateGroup, "Araby", root));

            Assert.That(paths.Keys, Is.EquivalentTo(new[] { RootNodeId }));
        }

        [Test]
        public void ATreeWithNoAudioAnywhereReachesNothing()
        {
            var paths = MusicSwitchContainerMergeService.BuildTreeKeyPaths(CreateVanillaBattleContainer(),
                Tree(BattleCultureStateGroup, "Araby", Node(RootNodeId)));

            Assert.That(paths, Is.Empty);
        }

        static MusicTree Tree(string stateGroupName, string stateName, MusicTreeNode root) =>
            new() { StateGroupName = stateGroupName, StateName = stateName, Root = root };

        static MusicTreeNode Node(int id, uint? audio = null, string value = null, string splitBy = null) => new()
        {
            Id = id,
            Value = value,
            SplitBy = splitBy,
            AudioFileIds = audio is uint audioFileId ? [audioFileId] : []
        };

        static List<uint> Keys(params string[] names) => [.. names.Select(WwiseHash.Compute)];

        /// <summary>What Wwise would pick for a runtime combination: the exact key at each level,
        /// otherwise the default.</summary>
        static uint Lookup(CAkMusicSwitchCntr_V136 container, params string[] values)
        {
            var node = container.AkDecisionTree.DecisionTree;
            foreach (var value in values)
            {
                node = node.Nodes.FirstOrDefault(child => child.Key == WwiseHash.Compute(value))
                    ?? node.Nodes.FirstOrDefault(child => child.Key == 0);
                if (node == null)
                    return 0;
            }
            return node.AudioNodeId;
        }

        // Culture > Key > Situation, every vanilla block with a default at the two lower levels.
        static CAkMusicSwitchCntr_V136 CreateContainerBelowTheCulture()
        {
            var container = new CAkMusicSwitchCntr_V136 { Id = 1234, HircType = AkBkHircType.Music_Switch, TreeDepth = 3 };
            foreach (var group in new[] { BattleCultureStateGroup, "Key", "Situation" })
                container.Arguments.Add(new AkGameSync_V136 { GroupId = WwiseHash.Compute(group), GroupType = AkGroupType.State });

            var audioNodeId = 300u;
            var root = new AkDecisionTree_V136.Node_V136();
            foreach (var culture in new[] { "Empire", "Cathay" })
            {
                var cultureNode = CreateBranch(WwiseHash.Compute(culture), 0);
                foreach (var key in new uint[] { WwiseHash.Compute("c"), WwiseHash.Compute("eb"), 0 })
                {
                    var keyNode = CreateBranch(key, 0);
                    foreach (var situation in new uint[] { WwiseHash.Compute("winning"), WwiseHash.Compute("losing"), 0 })
                        keyNode.Nodes.Add(CreateBranch(situation, audioNodeId++));
                    cultureNode.Nodes.Add(keyNode);
                }
                root.Nodes.Add(cultureNode);
            }

            DeclareLeavesAsChildren(container, root);
            container.AkDecisionTree = new AkDecisionTree_V136 { DecisionTree = root, Nodes = AkDecisionTree_V136.FlattenDecisionTree(root) };
            container.TreeDataSize = container.AkDecisionTree.GetSize();
            container.UpdateSectionSize();
            return container;
        }

        static Dictionary<string, uint> PathsOf(AkDecisionTree_V136.Node_V136 node, string prefix = "")
        {
            var paths = new Dictionary<string, uint>();
            foreach (var child in node.Nodes)
            {
                var path = $"{prefix}/{child.Key}";
                if (child.Nodes.Count == 0)
                    paths[path] = child.AudioNodeId;
                else
                    foreach (var (childPath, audioNodeId) in PathsOf(child, path))
                        paths[childPath] = audioNodeId;
            }
            return paths;
        }

        static CAkMusicSwitchCntr_V136 CreateVanillaAdaptiveContainer()
        {
            var container = new CAkMusicSwitchCntr_V136
            {
                Id = 67383790,
                HircType = AkBkHircType.Music_Switch,
                TreeDepth = 3
            };

            foreach (var group in new[] { "Battle_Phase", BattleCultureStateGroup, "Musical_Key" })
                container.Arguments.Add(new AkGameSync_V136 { GroupId = WwiseHash.Compute(group), GroupType = AkGroupType.State });

            var audioNodeId = 200u;
            var fight = CreateBranch(WwiseHash.Compute("fight"), 0);
            foreach (var culture in new[] { "Empire", "Cathay" })
            {
                var cultureNode = CreateBranch(WwiseHash.Compute(culture), 0);
                foreach (var key in new[] { "c", "eb" })
                    cultureNode.Nodes.Add(CreateBranch(WwiseHash.Compute(key), audioNodeId++));
                fight.Nodes.Add(cultureNode);
            }

            var bespokeCulture = CreateBranch(0, 0);
            bespokeCulture.Nodes.Add(CreateBranch(0, audioNodeId++));
            var bespoke = CreateBranch(WwiseHash.Compute("bespoke"), 0);
            bespoke.Nodes.Add(bespokeCulture);

            var root = new AkDecisionTree_V136.Node_V136 { Nodes = [fight, bespoke] };
            DeclareLeavesAsChildren(container, root);

            container.AkDecisionTree = new AkDecisionTree_V136
            {
                DecisionTree = root,
                Nodes = AkDecisionTree_V136.FlattenDecisionTree(root)
            };
            container.TreeDataSize = container.AkDecisionTree.GetSize();
            container.UpdateSectionSize();

            return container;
        }

        static void DeclareLeavesAsChildren(CAkMusicSwitchCntr_V136 container, AkDecisionTree_V136.Node_V136 root)
        {
            var childIds = new SortedSet<uint>(LeavesOf(root).Where(audioNodeId => audioNodeId != 0));
            container.MusicTransNodeParams.MusicNodeParams.Children = new Children_V136
            {
                NumChilds = (uint)childIds.Count,
                ChildIds = [.. childIds]
            };
        }

        static IEnumerable<uint> LeavesOf(AkDecisionTree_V136.Node_V136 node) =>
            node.Nodes.SelectMany(child => child.Nodes.Count == 0 ? [child.AudioNodeId] : LeavesOf(child));

        static CAkMusicSwitchCntr_V136 CreateVanillaBattleContainer()
        {
            var container = new CAkMusicSwitchCntr_V136
            {
                Id = BattleContainerId,
                HircType = AkBkHircType.Music_Switch,
                TreeDepth = 2
            };

            container.Arguments.Add(new AkGameSync_V136
            {
                GroupId = WwiseHash.Compute(BattleResultStateGroup),
                GroupType = AkGroupType.State
            });
            container.Arguments.Add(new AkGameSync_V136
            {
                GroupId = WwiseHash.Compute(BattleCultureStateGroup),
                GroupType = AkGroupType.State
            });

            var root = new AkDecisionTree_V136.Node_V136();
            var audioNodeId = 100u;

            foreach (var result in new[] { "lose", "win", "draw" })
            {
                var resultNode = CreateBranch(WwiseHash.Compute(result), 0);
                foreach (var culture in new[] { "Empire", "Cathay" })
                    resultNode.Nodes.Add(CreateBranch(WwiseHash.Compute(culture), audioNodeId++));

                root.Nodes.Add(resultNode);
            }

            DeclareLeavesAsChildren(container, root);

            container.AkDecisionTree = new AkDecisionTree_V136
            {
                DecisionTree = root,
                Nodes = AkDecisionTree_V136.FlattenDecisionTree(root)
            };
            container.TreeDataSize = container.AkDecisionTree.GetSize();
            container.UpdateSectionSize();

            return container;
        }

        // Stands in for the vanilla WH3_Campaign_Subcultures container: one argument, a default
        // branch and one culture, matching the shipped bank.
        static CAkMusicSwitchCntr_V136 CreateVanillaContainer()
        {
            var container = new CAkMusicSwitchCntr_V136
            {
                Id = SwitchContainerId,
                HircType = AkBkHircType.Music_Switch,
                TreeDepth = 1
            };

            container.Arguments.Add(new AkGameSync_V136
            {
                GroupId = WwiseHash.Compute("WH3_Campaign_Subcultures"),
                GroupType = AkGroupType.State
            });

            var root = new AkDecisionTree_V136.Node_V136
            {
                Nodes =
                [
                    CreateBranch(0, 338645341),
                    CreateBranch(WwiseHash.Compute("Dwarfs"), 1056018414)
                ]
            };

            DeclareLeavesAsChildren(container, root);

            container.AkDecisionTree = new AkDecisionTree_V136
            {
                DecisionTree = root,
                Nodes = AkDecisionTree_V136.FlattenDecisionTree(root)
            };
            container.TreeDataSize = container.AkDecisionTree.GetSize();
            container.UpdateSectionSize();

            return container;
        }

        static AkDecisionTree_V136.Node_V136 CreateBranch(uint key, uint audioNodeId)
        {
            return new AkDecisionTree_V136.Node_V136
            {
                Key = key,
                AudioNodeId = audioNodeId,
                Weight = Wh3MusicHierarchyInformation.BranchWeight,
                Probability = Wh3MusicHierarchyInformation.BranchProbability
            };
        }
    }
}

