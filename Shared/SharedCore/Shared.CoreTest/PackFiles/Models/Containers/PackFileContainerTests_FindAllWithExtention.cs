using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_FindAllWithExtention : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_FindAllWithExtention(Type containerType) : base(containerType) { }

        [Test]
        public void FindAllWithExtention_IsCaseInsensitive()
        {
            Assert.That(_container.FindAllWithExtention(".wem").Count, Is.EqualTo(3));
            Assert.That(_container.FindAllWithExtention(".WEM").Count, Is.EqualTo(3));
        }

        [Test]
        public void FindAllWithExtention_NoMatch_ReturnsEmpty()
        {
            Assert.That(_container.FindAllWithExtention(".xyz"), Is.Empty);
        }
    }
}
