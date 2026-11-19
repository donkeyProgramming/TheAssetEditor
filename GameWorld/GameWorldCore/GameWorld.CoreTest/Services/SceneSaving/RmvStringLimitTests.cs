using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Test.Services.SceneSaving
{
    [TestFixture]
    public class RmvStringLimitTests
    {
        [Test]
        public void TexturePath_AtTheLimit_IsStored()
        {
            var path = new string('a', RmvTexture.MaxPathLength);

            var texture = new RmvTexture { Path = path };

            Assert.That(texture.Path, Is.EqualTo(path));
        }

        [Test]
        public void TexturePath_OverTheLimit_ThrowsNamingThePath()
        {
            // Two bytes per character in UTF-8, so the byte count is what is limited
            var path = new string('é', 128);

            var ex = Assert.Throws<ArgumentException>(() => new RmvTexture { Path = path });

            Assert.That(ex!.Message, Does.Contain("256 bytes").And.Contain(path));
        }

        [Test]
        public void SkeletonName_AtTheLimit_IsStored()
        {
            var name = new string('a', RmvFileHeader.MaxSkeletonNameLength);

            var header = new RmvFileHeader { SkeletonName = name };

            Assert.That(header.SkeletonName, Is.EqualTo(name));
        }

        [Test]
        public void SkeletonName_OverTheLimit_ThrowsNamingTheSkeleton()
        {
            var name = new string('a', RmvFileHeader.MaxSkeletonNameLength + 1);

            var ex = Assert.Throws<ArgumentException>(() => new RmvFileHeader { SkeletonName = name });

            Assert.That(ex!.Message, Does.Contain("128 bytes").And.Contain(name));
        }
    }
}
