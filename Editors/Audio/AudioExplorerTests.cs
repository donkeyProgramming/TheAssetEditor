using Editors.Audio.AudioExplorer;

namespace Test.Audio
{
    [TestFixture]
    internal class AudioExplorerTests
    {
        [Test]
        public void AutoExpandNode_ExpandsFirstStatePathAndCompleteAudioSubtree()
        {
            var sourceActor = StateNode("VO_Actor", "source");
            var firstCulture = StateNode("VO_Culture", "first culture");
            var otherCulture = StateNode("VO_Culture", "other culture");
            var firstTargetActor = StateNode("VO_Actor", "first target");
            var otherTargetActor = StateNode("VO_Actor", "other target");

            var soundBesideContainer = Node("Sound 1");
            var randomContainer = Node("Random Container");
            var soundInContainer = Node("Sound 2");
            var nestedContainer = Node("Nested Random Container");
            var nestedSound = Node("Sound 3");

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

        private static HircTreeNode StateNode(string group, string value) => new() { DisplayName = $"State [{group}] - {value}", IsMetaNode = true };

        private static HircTreeNode Node(string displayName) => new() { DisplayName = displayName };
    }
}
