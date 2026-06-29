using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_SystemFilePath : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SystemFilePath(Type containerType) : base(containerType) { }

        [Test]
        public void SystemFilePath_MatchesSeededPath()
        {
            if (IsSystemFolderContainer)
            {
                Assert.That(_container.SystemFilePath, Does.StartWith(Path.GetTempPath().TrimEnd('\\')));
                Assert.That(_container.PackFileSettings.SaveLocationPath, Is.EqualTo(_container.SystemFilePath));
                return;
            }

            if (IsCachedContainer)
            {
                Assert.That(_container.SystemFilePath, Is.EqualTo(@"c:\game\data"));
                Assert.That(_container.PackFileSettings.SaveLocationPath, Is.Null);
                return;
            }

            Assert.That(_container.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(_container.PackFileSettings.SaveLocationPath, Is.EqualTo(_container.SystemFilePath));
        }
    }
}
