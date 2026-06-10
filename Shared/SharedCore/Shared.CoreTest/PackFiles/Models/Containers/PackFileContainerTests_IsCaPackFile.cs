using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_IsCaPackFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_IsCaPackFile(Type containerType) : base(containerType) { }

        [Test]
        public void IsCaPackFile_AlwaysStartsTrue()
        {
            if (IsSystemFolderContainer)
            {
                Assert.That(_container.IsCaPackFile, Is.False);
                return;
            }

            Assert.That(_container.IsCaPackFile, Is.True);
        }

        [Test]
        public void IsCaPackFile_SetterBehavior_MatchesContainerType()
        {
            if (IsSystemFolderContainer)
            {
                // SystemFolderContainer starts as false and setter works normally
                _container.IsCaPackFile = true;
                Assert.That(_container.IsCaPackFile, Is.True);
                return;
            }

            _container.IsCaPackFile = false;
            Assert.That(_container.IsCaPackFile, Is.EqualTo(IsCachedContainer));
        }
    }
}
