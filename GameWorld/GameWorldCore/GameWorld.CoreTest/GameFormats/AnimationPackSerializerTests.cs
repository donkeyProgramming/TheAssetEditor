using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;

namespace GameWorld.Core.Test.GameFormats
{
    public class AnimationPackSerializerTests
    {
        // Ported from Szy-Cathay/TheAssetEditor-CN 959607d6f
        [Test]
        public void ChildThatFailsToParse_KeepsOnlyItsOwnBytes()
        {
            byte[] brokenChild = [0xDE, 0xAD, 0xBE, 0xEF];
            byte[] otherChild = [0x11, 0x22, 0x33];
            var source = new AnimationPackFileDatabase("test.animpack");
            source.AddFile(new UnknownAnimFile("broken.bin", brokenChild));     // .bin at the root goes to the db parser, which fails on this
            source.AddFile(new UnknownAnimFile("other.unknown", otherChild));
            var packBytes = AnimationPackSerializer.ConvertToBytes(source);
            var packFile = PackFile.CreateFromBytes("test.animpack", packBytes);
            var packFileService = new Mock<IPackFileService>();
            packFileService.Setup(x => x.GetFullPath(packFile, It.IsAny<IPackFileContainer?>())).Returns("test.animpack");

            var loaded = AnimationPackSerializer.Load(packFile, packFileService.Object);

            var files = loaded.Files.ToArray();
            Assert.That(files, Has.Length.EqualTo(2));
            Assert.That(files[0], Is.InstanceOf<UnknownAnimFile>());
            Assert.That(files[0].ToByteArray(), Is.EqualTo(brokenChild));
            Assert.That(files[1].ToByteArray(), Is.EqualTo(otherChild));
            Assert.That(AnimationPackSerializer.ConvertToBytes(loaded), Is.EqualTo(packBytes));
        }
    }
}
