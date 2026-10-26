using Editors.Audio.Shared.Wwise.Generators;
using Shared.ByteParsing;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    // Adding a culture to the ambient fragments Switch container. The shape asserted here was read
    // out of campaign_music__core.bnk with MusicEventShapeResearch.DumpTheFragmentsSwitchContainer:
    // a faction container whose children are themselves Switch containers keyed on the musical key
    // the score is in, and two cultures pointing at the same child where vanilla shares a fragment
    // set between them. Below the key, a culture splits again on call or response.
    internal class AmsFragmentMergeTests
    {
        const uint FactionContainerId = 418295225;
        const uint KeyGroupId = 2939663928;
        const uint NewKeySwitchId = 5000;
        static readonly uint CallResponseGroupId = WwiseHash.Compute("WH3_Campaign_Music_AMS_Fragments_CallResponse");

        [Test]
        public void ANewCultureBecomesASwitchAndAChild()
        {
            var merged = new AmsFragmentMergeService()
                .AddCultures(CreateVanillaContainer(), [new AmsFragmentBranch("Araby", NewKeySwitchId)]);

            Assert.Multiple(() =>
            {
                var switchPackage = merged.SwitchList
                    .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                    .Single(package => package.SwitchId == WwiseHash.Compute("Araby"));

                Assert.That(switchPackage.NodeIdList, Is.EqualTo(new[] { NewKeySwitchId }));

                // A switch pointing at a node the container does not claim as a child is not
                // resolved, so the two lists have to move together.
                Assert.That(merged.Children.ChildIds, Does.Contain(NewKeySwitchId));
                Assert.That(merged.Children.NumChilds, Is.EqualTo((uint)merged.Children.ChildIds.Count));
                Assert.That(merged.NumSwitchGroups, Is.EqualTo((uint)merged.SwitchList.Count));
            });
        }

        [Test]
        public void TheVanillaCulturesAreKept()
        {
            var vanillaContainer = CreateVanillaContainer();
            var merged = new AmsFragmentMergeService()
                .AddCultures(vanillaContainer, [new AmsFragmentBranch("Araby", NewKeySwitchId)]);

            Assert.Multiple(() =>
            {
                foreach (var vanillaSwitch in vanillaContainer.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
                {
                    var mergedSwitch = merged.SwitchList
                        .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                        .SingleOrDefault(package => package.SwitchId == vanillaSwitch.SwitchId);

                    Assert.That(mergedSwitch, Is.Not.Null, $"vanilla switch {vanillaSwitch.SwitchId} was dropped");
                    Assert.That(mergedSwitch.NodeIdList, Is.EqualTo(vanillaSwitch.NodeIdList));
                }

                Assert.That(merged.DefaultSwitch, Is.EqualTo(vanillaContainer.DefaultSwitch));
                Assert.That(merged.GroupId, Is.EqualTo(vanillaContainer.GroupId));
            });
        }

        [Test]
        public void MergingDoesNotEditTheVanillaContainer()
        {
            var vanillaContainer = CreateVanillaContainer();
            new AmsFragmentMergeService().AddCultures(vanillaContainer, [new AmsFragmentBranch("Araby", NewKeySwitchId)]);

            Assert.Multiple(() =>
            {
                Assert.That(vanillaContainer.SwitchList, Has.Count.EqualTo(3));
                Assert.That(vanillaContainer.Children.ChildIds, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void TheMergedContainerSurvivesAWriteReadRoundTrip()
        {
            var merged = new AmsFragmentMergeService()
                .AddCultures(CreateVanillaContainer(), [new AmsFragmentBranch("Araby", NewKeySwitchId)]);

            var reloaded = new CAkSwitchCntr_V136();
            reloaded.ReadHirc(new ByteChunk(merged.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(FactionContainerId));
                Assert.That(reloaded.GroupId, Is.EqualTo(merged.GroupId));
                Assert.That(reloaded.DefaultSwitch, Is.EqualTo(merged.DefaultSwitch));
                Assert.That(reloaded.SwitchList, Has.Count.EqualTo(4));
                Assert.That(reloaded.Children.ChildIds, Is.EqualTo(merged.Children.ChildIds));
                Assert.That(reloaded.Parameters, Has.Count.EqualTo(merged.Parameters.Count));

                var reloadedSwitch = reloaded.SwitchList
                    .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                    .Single(package => package.SwitchId == WwiseHash.Compute("Araby"));
                Assert.That(reloadedSwitch.NodeIdList, Is.EqualTo(new[] { NewKeySwitchId }));
            });
        }

        [Test]
        public void TheKeySwitchMirrorsTheKeysAnExistingCultureUses()
        {
            // Which keys the score uses is the game's business, so they are copied from a sibling
            // rather than written down - a table would go stale the moment a patch added one.
            var keySwitch = new AmsFragmentMergeService().CreateKeySwitchContainer(
                CreateVanillaContainer(), new SiblingLookup(), NewKeySwitchId, KeyTargets);

            Assert.Multiple(() =>
            {
                Assert.That(keySwitch.Id, Is.EqualTo(NewKeySwitchId));
                Assert.That(keySwitch.GroupId, Is.EqualTo(KeyGroupId));
                Assert.That(keySwitch.DefaultSwitch, Is.EqualTo(WwiseHash.Compute("None")));
                Assert.That(keySwitch.NodeBaseParams.DirectParentId, Is.EqualTo(FactionContainerId));
                Assert.That(PackagesOf(keySwitch), Is.EquivalentTo(new Dictionary<uint, uint>
                {
                    [WwiseHash.Compute("C_01")] = 6000,
                    [WwiseHash.Compute("C_02")] = 6000,
                    [WwiseHash.Compute("Eb_01")] = 6001
                }));
                Assert.That(keySwitch.Children.ChildIds, Is.EqualTo(new[] { 6000u, 6001u }));
            });
        }

        // Vanilla's None key plays a silent placeholder. Leaving it out, with the default kept as
        // None, plays nothing the same way, and so does a key the tree gave nothing.
        [Test]
        public void AKeyGivenNothingIsLeftOut()
        {
            var keySwitch = new AmsFragmentMergeService().CreateKeySwitchContainer(
                CreateVanillaContainer(), new SiblingLookup(), NewKeySwitchId, KeyTargets);

            Assert.That(PackagesOf(keySwitch).Keys, Does.Not.Contain(WwiseHash.Compute("None")));
        }

        [Test]
        public void TheCallResponseSwitchIsMirroredFromVanillasAndPointsCallAndResponseApart()
        {
            var callResponse = new AmsFragmentMergeService().CreateCallResponseSwitchContainer(
                CreateVanillaContainer(), new SiblingLookup(), 6000, NewKeySwitchId, callTargetId: 7000, responseTargetId: 7001);

            Assert.Multiple(() =>
            {
                Assert.That(callResponse.GroupId, Is.EqualTo(CallResponseGroupId));
                Assert.That(callResponse.NodeBaseParams.DirectParentId, Is.EqualTo(NewKeySwitchId));
                Assert.That(PackagesOf(callResponse), Is.EquivalentTo(new Dictionary<uint, uint>
                {
                    [WwiseHash.Compute("Call")] = 7000,
                    [WwiseHash.Compute("Response")] = 7001
                }));
                Assert.That(callResponse.Children.ChildIds, Is.EqualTo(new[] { 7000u, 7001u }));
            });
        }

        [Test]
        public void ACallWithNoResponseLeavesTheResponseSilent()
        {
            var callResponse = new AmsFragmentMergeService().CreateCallResponseSwitchContainer(
                CreateVanillaContainer(), new SiblingLookup(), 6000, NewKeySwitchId, callTargetId: 7000, responseTargetId: 0);

            Assert.That(PackagesOf(callResponse).Keys, Is.EqualTo(new[] { WwiseHash.Compute("Call") }));
        }

        [Test]
        public void TheSwitchesSurviveAWriteReadRoundTrip()
        {
            var service = new AmsFragmentMergeService();
            var keySwitch = service.CreateKeySwitchContainer(CreateVanillaContainer(), new SiblingLookup(), NewKeySwitchId, KeyTargets);
            var callResponse = service.CreateCallResponseSwitchContainer(CreateVanillaContainer(), new SiblingLookup(), 6000, NewKeySwitchId, 7000, 7001);

            foreach (var original in new[] { keySwitch, callResponse })
            {
                var reloaded = new CAkSwitchCntr_V136();
                reloaded.ReadHirc(new ByteChunk(original.WriteData()));

                Assert.Multiple(() =>
                {
                    Assert.That(reloaded.Id, Is.EqualTo(original.Id));
                    Assert.That(reloaded.GroupId, Is.EqualTo(original.GroupId));
                    Assert.That(PackagesOf(reloaded), Is.EquivalentTo(PackagesOf(original)));
                    Assert.That(reloaded.Children.ChildIds, Is.EqualTo(original.Children.ChildIds));
                });
            }
        }

        [Test]
        public void AContainerWithNoSiblingToCopyKeysFromIsRefused()
        {
            Assert.That(
                () => new AmsFragmentMergeService().CreateKeySwitchContainer(CreateVanillaContainer(), new EmptyLookup(), NewKeySwitchId, KeyTargets),
                Throws.TypeOf<NotSupportedException>());
        }

        /// <summary>C plays one call/response switch, Eb another, and None nothing.</summary>
        static uint KeyTargets(uint keySwitchId) =>
            keySwitchId == WwiseHash.Compute("C_01") || keySwitchId == WwiseHash.Compute("C_02") ? 6000u :
            keySwitchId == WwiseHash.Compute("Eb_01") ? 6001u :
            0;

        static Dictionary<uint, uint> PackagesOf(CAkSwitchCntr_V136 container) =>
            container.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>().ToDictionary(package => package.SwitchId, package => package.NodeIdList.Single());

        [Test]
        public void CombiningTwoModsKeepsBothCultures()
        {
            var vanillaContainer = CreateVanillaContainer();
            var service = new AmsFragmentMergeService();

            var firstMod = service.AddCultures(vanillaContainer, [new AmsFragmentBranch("Araby", 5000)]);
            var secondMod = service.AddCultures(vanillaContainer, [new AmsFragmentBranch("Nippon", 5001)]);

            var merged = service.MergeContainers(vanillaContainer, firstMod);
            merged = service.MergeContainers(merged, secondMod);

            var switchIds = merged.SwitchList
                .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                .Select(package => package.SwitchId)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(switchIds, Does.Contain(WwiseHash.Compute("Araby")));
                Assert.That(switchIds, Does.Contain(WwiseHash.Compute("Nippon")));
                Assert.That(merged.Children.ChildIds, Does.Contain(5000u));
                Assert.That(merged.Children.ChildIds, Does.Contain(5001u));
            });
        }

        [Test]
        public void TheBaseWinsACultureBothModsClaim()
        {
            var vanillaContainer = CreateVanillaContainer();
            var service = new AmsFragmentMergeService();

            var firstMod = service.AddCultures(vanillaContainer, [new AmsFragmentBranch("Araby", 5000)]);
            var secondMod = service.AddCultures(vanillaContainer, [new AmsFragmentBranch("Araby", 5001)]);

            var merged = service.MergeContainers(vanillaContainer, firstMod);
            merged = service.MergeContainers(merged, secondMod);

            var arabySwitch = merged.SwitchList
                .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                .Single(package => package.SwitchId == WwiseHash.Compute("Araby"));

            Assert.That(arabySwitch.NodeIdList, Is.EqualTo(new[] { 5000u }));
        }

        /// <summary>Cut down from the real thing: two cultures sharing one fragment set, which is
        /// what vanilla does for Empire and WH1_2, plus the 'None' default.</summary>
        static CAkSwitchCntr_V136 CreateVanillaContainer()
        {
            var container = new CAkSwitchCntr_V136
            {
                Id = FactionContainerId,
                HircType = AkBkHircType.SwitchContainer,
                EGroupType = AkGroupType.State,
                GroupId = WwiseHash.Compute("WH3_Campaign_Music_AMS_Fragments_Faction"),
                DefaultSwitch = WwiseHash.Compute("None"),
                NodeBaseParams = new NodeBaseParams_V136()
            };

            container.Children.ChildIds.AddRange([1001u, 1002u]);
            container.Children.NumChilds = 2;

            foreach (var (switchName, nodeId) in new[] { ("None", 1001u), ("Empire", 1002u), ("WH1_2", 1002u) })
            {
                container.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136
                {
                    SwitchId = WwiseHash.Compute(switchName),
                    NodeIdList = [nodeId]
                });
            }

            container.NumSwitchGroups = 3;
            return container;
        }

        /// <summary>A vanilla culture's key switch and the call/response switch under it, which are
        /// what a new culture's are modelled on.</summary>
        sealed class SiblingLookup : IAudioRepositoryLookup
        {
            public CAkSwitchCntr_V136 FindSwitchContainer(uint id) => id switch
            {
                1001 => CreateSwitch(1001, KeyGroupId, ["None", "C_01", "C_02", "Eb_01"], 2001),
                2001 => CreateSwitch(2001, CallResponseGroupId, ["None", "Call", "Response"], 3001),
                _ => null
            };

            static CAkSwitchCntr_V136 CreateSwitch(uint id, uint groupId, string[] switchNames, uint nodeId)
            {
                var container = new CAkSwitchCntr_V136
                {
                    Id = id,
                    EGroupType = AkGroupType.State,
                    GroupId = groupId,
                    DefaultSwitch = WwiseHash.Compute("None"),
                    NodeBaseParams = new NodeBaseParams_V136()
                };

                foreach (var switchName in switchNames)
                    container.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136 { SwitchId = WwiseHash.Compute(switchName), NodeIdList = [nodeId] });

                return container;
            }
        }

        sealed class EmptyLookup : IAudioRepositoryLookup
        {
            public CAkSwitchCntr_V136 FindSwitchContainer(uint id) => null;
        }
    }
}
