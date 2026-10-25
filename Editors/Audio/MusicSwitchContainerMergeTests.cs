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

        [Test]
        public void TheContainerForTheMergingSoundBankCarriesOnlyTheModsBranches()
        {
            // The merging .bnk is combined with other mods' before vanilla is folded in, so carrying
            // vanilla here would make every mod look like it had deliberately set the same branches
            // and the merger could not tell which mod actually claimed a culture.
            var modded = new MusicSwitchContainerMergeService()
                .CreateModdedContainer(CreateVanillaContainer(), [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);

            var branch = modded.AkDecisionTree.DecisionTree.Nodes.Single();

            Assert.Multiple(() =>
            {
                Assert.That(branch.Key, Is.EqualTo(WwiseHash.Compute("Araby")));
                Assert.That(branch.AudioNodeId, Is.EqualTo(NewRanSeqId));

                // Everything outside the tree still has to be vanilla's, or the game is being handed
                // a container it never asked for.
                Assert.That(modded.Id, Is.EqualTo(SwitchContainerId));
                Assert.That(modded.Arguments.Single().GroupId, Is.EqualTo(WwiseHash.Compute("WH3_Campaign_Subcultures")));
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
            var selfMerged = service.MergeContainers(vanillaContainer, vanillaContainer);

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
        public void CombiningTwoModsKeepsBothSubcultures()
        {
            // What the merger does with two modders' merging .bnks. Each adds a subculture vanilla
            // does not have, which is the normal case - nothing collides, and both have to come
            // through alongside everything vanilla already covers.
            var mergeService = new MusicSwitchContainerMergeService();
            var vanillaContainer = CreateVanillaContainer();

            var firstMod = mergeService.CreateModdedContainer(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Araby", NewRanSeqId)]);
            var secondMod = mergeService.CreateModdedContainer(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Cathay", 6000)]);

            var merged = mergeService.MergeContainers(
                mergeService.MergeContainers(vanillaContainer, firstMod), secondMod);

            var branchesByKey = merged.AkDecisionTree.DecisionTree.Nodes.ToDictionary(node => node.Key);

            Assert.Multiple(() =>
            {
                Assert.That(branchesByKey[WwiseHash.Compute("Araby")].AudioNodeId, Is.EqualTo(NewRanSeqId));
                Assert.That(branchesByKey[WwiseHash.Compute("Cathay")].AudioNodeId, Is.EqualTo(6000u));
                Assert.That(branchesByKey[WwiseHash.Compute("Dwarfs")].AudioNodeId, Is.EqualTo(1056018414u));
                Assert.That(branchesByKey.ContainsKey(0u), "the default branch survives");
            });
        }

        [Test]
        public void VanillaWinsAStateAModAlsoClaims()
        {
            // Only reachable if a mod deliberately keys a branch on a subculture vanilla already
            // has. The Dialogue Event merge breaks the tie this way, so this one does too.
            var mergeService = new MusicSwitchContainerMergeService();
            var vanillaContainer = CreateVanillaContainer();

            var modded = mergeService.CreateModdedContainer(vanillaContainer, [new MusicBranch(SubcultureStateGroup, "Dwarfs", NewRanSeqId)]);
            var merged = mergeService.MergeContainers(vanillaContainer, modded);

            var dwarfBranch = merged.AkDecisionTree.DecisionTree.Nodes
                .Single(node => node.Key == WwiseHash.Compute("Dwarfs"));

            Assert.That(dwarfBranch.AudioNodeId, Is.EqualTo(1056018414u));
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
            var modded = new MusicSwitchContainerMergeService()
                .CreateModdedContainer(CreateVanillaBattleContainer(), [new MusicBranch(BattleCultureStateGroup, "Araby", NewRanSeqId)]);

            Assert.Multiple(() =>
            {
                foreach (var resultNode in modded.AkDecisionTree.DecisionTree.Nodes)
                {
                    Assert.That(resultNode.AudioNodeId, Is.EqualTo(0u), "a result node is not a leaf");
                    Assert.That(resultNode.Nodes, Has.Count.EqualTo(1));
                }
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
        // branch and one culture, matching what MusicEventShapeResearch reports.
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

