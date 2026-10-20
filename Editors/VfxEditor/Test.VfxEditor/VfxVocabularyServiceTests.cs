using Editors.VfxEditor.Vocabulary;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Test.VfxEditor
{
    class VfxVocabularyServiceTests
    {
        static PackFileService Service() => new(null) { EnforceGameFilesMustBeLoaded = false };

        static void AddCaPack(PackFileService service, params PackFile[] files)
        {
            var pack = service.CreateNewPackFileContainer("ca", PackFileVersion.PFH5, PackFileCAType.RELEASE);
            service.AddFilesToPack(pack, files.Select(f => new NewPackFileEntry("vfx", f)).ToList());
            pack.IsCaPackFile = true;
        }

        static PackFile Effect(string name) => PackFile.CreateFromBytes(name + ".xml", Fixtures.Utf16(Fixtures.Default));

        [Test]
        public void A_file_that_cannot_be_read_adds_nothing()
        {
            var unreadable = new Mock<IDataSource>();
            unreadable.Setup(s => s.ReadData()).Throws(new System.IO.IOException("the pack moved"));
            var service = Service();
            AddCaPack(service, Effect("a"), new PackFile("b.xml", unreadable.Object));

            Assert.That(new VfxVocabularyService(service).Get().Result.EffectNames, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void An_empty_scan_is_not_kept()
        {
            var service = Service();
            var vocabularies = new VfxVocabularyService(service);
            Assert.That(vocabularies.Get().Result.EffectNames, Is.Empty);

            AddCaPack(service, Effect("a"));
            Assert.That(vocabularies.Get().Result.EffectNames, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void A_failed_scan_is_not_kept()
        {
            var service = Service();
            AddCaPack(service, Effect("a"));
            var flaky = new Mock<IPackFileService>();
            flaky.SetupSequence(s => s.GetAllPackfileContainers())
                .Throws(new InvalidOperationException("a pack was added mid-scan"))
                .Returns(service.GetAllPackfileContainers);
            flaky.Setup(s => s.FindAllWithExtention(It.IsAny<string>(), It.IsAny<IPackFileContainer?>()))
                .Returns((string extension, IPackFileContainer? pack) => service.FindAllWithExtention(extension, pack));
            var vocabularies = new VfxVocabularyService(flaky.Object);
            Assert.That(() => vocabularies.Get().Result, Throws.InstanceOf<AggregateException>());

            Assert.That(vocabularies.Get().Result.EffectNames, Is.EqualTo(new[] { "a" }));
        }
    }
}
