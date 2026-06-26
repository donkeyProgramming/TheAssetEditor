using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_SearchFiles : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SearchFiles(Type containerType) : base(containerType) { }

        [Test]
        public void SearchFiles_NullFilters_ReturnsAll()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(18));
        }

        [Test]
        public void SearchFiles_TextAndExtension_FiltersCorrectly()
        {
            var results = _container.SearchFiles("battle", [".wem"]);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Path, Is.EqualTo(@"audio\battle_sound.wem"));
        }

        [Test]
        public void SearchFiles_ResultsArePathSorted()
        {
            var paths = _container.SearchFiles(null, null).Select(x => x.Path).ToList();
            var sorted = paths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(paths, Is.EqualTo(sorted));
        }

        [Test]
        public void SearchFiles_EmptyExtensionList_TreatedAsNoFilter()
        {
            var results = _container.SearchFiles(null, []);
            Assert.That(results.Count, Is.EqualTo(18));
        }

        [Test]
        public void SearchFiles_ExtensionFilter_MatchesFilenameSubstrings_ForLegacyWemVariants()
        {
            var results = _container.SearchFiles(null, [".wem"]);
            var paths = results.Select(x => x.Path).ToList();

            Assert.That(paths, Does.Contain(@"audio\sound.wem"));
            Assert.That(paths, Does.Contain(@"audio\voice.wem_temp"));
            Assert.That(paths, Does.Contain(@"audio\voice.wem.{sdf}"));
            Assert.That(paths, Does.Not.Contain(@"audio\voice.txt"));
        }
    }
}
