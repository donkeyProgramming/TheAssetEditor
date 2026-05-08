using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_IsCaPackFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_IsCaPackFile(Type containerType) : base(containerType) { }

        [Test]
        public void IsCaPackFile_AlwaysStartsTrue()
        {
            Assert.That(_container.IsCaPackFile, Is.True);
        }

        [Test]
        public void IsCaPackFile_SetterBehavior_MatchesContainerType()
        {
            _container.IsCaPackFile = false;
            Assert.That(_container.IsCaPackFile, Is.EqualTo(IsCachedContainer));
        }
    }
}
