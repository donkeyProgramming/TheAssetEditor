using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_SaveToDisk : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SaveToDisk(Type containerType) : base(containerType) { }

        [Test]
        public void SaveToDisk_CachedContainer_Throws()
        {
            if (!IsCachedContainer)
                Assert.Ignore("Writable pack SaveToDisk behavior is validated in serializer/integration tests.");

            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => _container.SaveToDisk("path", false, gameInfo));
        }
    }
}
