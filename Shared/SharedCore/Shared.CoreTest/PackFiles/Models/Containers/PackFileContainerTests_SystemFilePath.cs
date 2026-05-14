using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_SystemFilePath : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SystemFilePath(Type containerType) : base(containerType) { }

        [Test]
        public void SystemFilePath_MatchesSeededPath()
        {
            Assert.That(_container.SystemFilePath, Is.EqualTo(@"c:\game\data"));
        }
    }
}
