using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_SearchFiles : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SearchFiles(Type containerType)
            : base(containerType)
        {
        }

        [Test]
        public void NullFilters_ReturnsAllFiles()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(15));
        }

        [Test]
        public void TextFilter_MatchesFileName()
        {
            var results = _container.SearchFiles("unit", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.model"));
        }

        [Test]
        public void TextFilter_IsCaseInsensitive()
        {
            var results = _container.SearchFiles("UNIT", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.model"));
        }

        [Test]
        public void TextFilter_PartialMatch()
        {
            var results = _container.SearchFiles("battle", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("battle_sound.wem"));
        }

        [Test]
        public void TextFilter_NoMatch_ReturnsEmpty()
        {
            var results = _container.SearchFiles("nonexistent", null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ExtensionFilter_SingleExtension()
        {
            var results = _container.SearchFiles(null, [".wem"]);
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.All(r => r.File.Name.EndsWith(".wem")), Is.True);
        }

        [Test]
        public void ExtensionFilter_MultipleExtensions()
        {
            var results = _container.SearchFiles(null, [".wem", ".lua"]);
            Assert.That(results.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExtensionFilter_NoMatch_ReturnsEmpty()
        {
            var results = _container.SearchFiles(null, [".xyz"]);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void CombinedFilters_TextAndExtension()
        {
            var results = _container.SearchFiles("battle", [".wem"]);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("battle_sound.wem"));
        }

        [Test]
        public void CombinedFilters_TextMatchesButExtensionDoesNot_ReturnsEmpty()
        {
            var results = _container.SearchFiles("unit", [".wem"]);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ResultsAreSortedByPath()
        {
            var results = _container.SearchFiles(null, null);
            var paths = results.Select(r => r.Path).ToList();
            var sorted = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(paths, Is.EqualTo(sorted));
        }

        [Test]
        public void ResultsContainCorrectPaths()
        {
            var results = _container.SearchFiles("diffuse", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Path, Is.EqualTo("models\\textures\\diffuse.dds"));
        }

        [Test]
        public void EmptyExtensionList_TreatedAsNoFilter()
        {
            var results = _container.SearchFiles(null, []);
            Assert.That(results.Count, Is.EqualTo(15));
        }
    }
}
