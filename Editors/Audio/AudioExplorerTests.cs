using Editors.Audio.AudioExplorer;

using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Moq;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    [TestFixture]
    internal class AudioExplorerTests
    {
        [Test]
        public void AutoExpandNode_ExpandsFirstStatePathAndCompleteAudioSubtree()
        {
            var sourceActor = new HircTreeNode { DisplayName = "State [VO_Actor] - source", IsMetaNode = true };
            var firstCulture = new HircTreeNode { DisplayName = "State [VO_Culture] - first culture", IsMetaNode = true };
            var otherCulture = new HircTreeNode { DisplayName = "State [VO_Culture] - other culture", IsMetaNode = true };
            var firstTargetActor = new HircTreeNode { DisplayName = "State [VO_Actor] - first target", IsMetaNode = true };
            var otherTargetActor = new HircTreeNode { DisplayName = "State [VO_Actor] - other target", IsMetaNode = true };

            var soundBesideContainer = new HircTreeNode { DisplayName = "Sound 1" };
            var randomContainer = new HircTreeNode { DisplayName = "Random Container" };
            var soundInContainer = new HircTreeNode { DisplayName = "Sound 2" };
            var nestedContainer = new HircTreeNode { DisplayName = "Nested Random Container" };
            var nestedSound = new HircTreeNode { DisplayName = "Sound 3" };

            sourceActor.Children.Add(firstCulture);
            sourceActor.Children.Add(otherCulture);
            firstCulture.Children.Add(firstTargetActor);
            firstCulture.Children.Add(otherTargetActor);
            firstTargetActor.Children.Add(soundBesideContainer);
            firstTargetActor.Children.Add(randomContainer);
            randomContainer.Children.Add(soundInContainer);
            randomContainer.Children.Add(nestedContainer);
            nestedContainer.Children.Add(nestedSound);

            AudioExplorerViewModel.AutoExpandNode(sourceActor);

            Assert.Multiple(() =>
            {
                Assert.That(sourceActor.IsExpanded, Is.True);
                Assert.That(firstCulture.IsExpanded, Is.True);
                Assert.That(firstTargetActor.IsExpanded, Is.True);
                Assert.That(soundBesideContainer.IsExpanded, Is.True);
                Assert.That(randomContainer.IsExpanded, Is.True);
                Assert.That(soundInContainer.IsExpanded, Is.True);
                Assert.That(nestedContainer.IsExpanded, Is.True);
                Assert.That(nestedSound.IsExpanded, Is.True);
                Assert.That(otherCulture.IsExpanded, Is.False);
                Assert.That(otherTargetActor.IsExpanded, Is.False);
            });
        }

        [Test]
        public void ParentParser_ResolvesParentUsingReferringBnkPath()
        {
            const string BnkPath = @"audio\wwise\test.bnk";
            const uint ParentId = 2;
            var sound = new CAkSound_V136
            {
                Id = 1,
                HircType = AkBkHircType.Sound,
                BnkFilePath = BnkPath,
                NodeBaseParams = new NodeBaseParams_V136 { DirectParentId = ParentId }
            };
            var parent = new CAkActorMixer_V136
            {
                Id = ParentId,
                HircType = AkBkHircType.ActorMixer,
                BnkFilePath = BnkPath,
                NodeBaseParams = new NodeBaseParams_V136 { DirectParentId = 0 }
            };
            var repository = new Mock<IAudioRepository>();
            repository
                .Setup(x => x.ResolveHircReferences(It.IsAny<IReadOnlyCollection<HircReferenceRequest>>()))
                .Returns((IReadOnlyCollection<HircReferenceRequest> requests) =>
                {
                    var request = requests.Single();
                    return new Dictionary<HircReferenceRequest, Shared.GameFormats.Wwise.Hirc.HircItem> { [request] = parent };
                });

            var nodes = new HircTreeParentParser(repository.Object).BuildHierarchyAsFlatList(sound);

            repository.Verify(
                x => x.ResolveHircReferences(
                    It.Is<IReadOnlyCollection<HircReferenceRequest>>(
                        requests => requests.Contains(new HircReferenceRequest(ParentId, BnkPath)))),
                Times.Once);
            Assert.That(nodes.Select(node => node.Hirc), Does.Contain(parent));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChildrenParser_ResolvesSiblingReferencesInOneBankAwareBatch(bool lazyLoadChildren)
        {
            const string BnkPath = @"audio\wwise\test.bnk";
            var root = new CAkActorMixer_V136
            {
                Id = 1,
                HircType = AkBkHircType.ActorMixer,
                BnkFilePath = BnkPath,
                Children = new Children_V136 { ChildIds = [2, 3] }
            };
            var firstChild = new CAkActorMixer_V136 { Id = 2, HircType = AkBkHircType.ActorMixer, BnkFilePath = BnkPath };
            var secondChild = new CAkActorMixer_V136 { Id = 3, HircType = AkBkHircType.ActorMixer, BnkFilePath = BnkPath };
            var repository = new Mock<IAudioRepository>();
            repository
                .Setup(x => x.ResolveHircReferences(It.IsAny<IReadOnlyCollection<HircReferenceRequest>>()))
                .Returns((IReadOnlyCollection<HircReferenceRequest> requests) => requests.ToDictionary(
                    request => request,
                    request => request.HircId == firstChild.Id
                        ? (Shared.GameFormats.Wwise.Hirc.HircItem)firstChild
                        : secondChild));

            var tree = new HircTreeChildrenParser(repository.Object, lazyLoadChildren).BuildHierarchy(root);
            if (lazyLoadChildren)
                tree.IsExpanded = true;

            repository.Verify(
                x => x.ResolveHircReferences(
                    It.Is<IReadOnlyCollection<HircReferenceRequest>>(
                        requests => requests.Count == 2 &&
                                    requests.Contains(new HircReferenceRequest(2, BnkPath)) &&
                                    requests.Contains(new HircReferenceRequest(3, BnkPath)))),
                Times.Once);
            Assert.That(tree.Children.Select(node => node.Hirc), Is.EquivalentTo(new[] { firstChild, secondChild }));
        }
    }
}
