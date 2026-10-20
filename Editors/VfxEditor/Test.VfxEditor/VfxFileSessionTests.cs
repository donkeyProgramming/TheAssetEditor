using Editors.VfxEditor;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Test.TestingUtility.TestUtility;

namespace Test.VfxEditor
{
    class VfxFileSessionTests
    {
        PackFileService _packFileService = null!;
        IPackFileContainer _output = null!;
        VfxFileSession _session = null!;

        [SetUp]
        public void Setup()
        {
            _packFileService = new PackFileService(null) { EnforceGameFilesMustBeLoaded = false };
            var source = _packFileService.CreateNewPackFileContainer("source", PackFileVersion.PFH5, PackFileCAType.MOD);
            _packFileService.AddFilesToPack(source, [
                new NewPackFileEntry("vfx", PackFile.CreateFromBytes("fx.xml", Fixtures.Utf16(Fixtures.Default))),
                new NewPackFileEntry("vfx", PackFile.CreateFromBytes("lib.xml", Fixtures.Utf16(Fixtures.Effect(emitters: Fixtures.Emitters("t1", "t2"))))),
                new NewPackFileEntry("vfx\\materials", PackFile.CreateFromBytes("m.xml", "<PROPERTY_SET/>"u8.ToArray())),
                new NewPackFileEntry("vfx", PackFile.CreateFromBytes("broken.xml", "<vfx>"u8.ToArray()))]);
            _output = _packFileService.CreateNewPackFileContainer("output", PackFileVersion.PFH5, PackFileCAType.MOD, setEditablePack: true);
            _session = new VfxFileSession(_packFileService, new FileSaveService(_packFileService, new Mock<IStandardDialogs>().Object, MockScopedLogger.Create()));
        }

        PackFile File(string path) => _packFileService.FindFile(path)!;

        [Test]
        public void Save_writes_the_edited_bytes_into_the_editable_pack_in_the_files_encoding()
        {
            _session.Load(File("vfx\\fx.xml"));
            var document = _session.Document!;
            document.Apply([document.SetAttribute(Fixtures.Emitter(document), "type", "continuous_rigid")]);
            Assert.That(_session.HasUnsavedChanges, Is.True);

            Assert.That(_session.Save(), Is.True);

            var saved = _packFileService.FindFile("vfx\\fx.xml", _output)!;
            Assert.That(saved.DataSource.ReadData(), Is.EqualTo(document.Save()));
            Assert.That(saved.DataSource.ReadData()[..2], Is.EqualTo(new byte[] { 0xFF, 0xFE }));
            Assert.That(_session.HasUnsavedChanges, Is.False);
            Assert.That(_session.File, Is.SameAs(saved));
        }

        [Test]
        public void A_second_save_updates_the_same_file()
        {
            _session.Load(File("vfx\\fx.xml"));
            _session.Save();
            var document = _session.Document!;
            document.Apply([document.SetAttribute(Fixtures.Emitter(document), "type", "x")]);
            _session.Save();
            Assert.That(_output.GetFileCount(), Is.EqualTo(1));
            Assert.That(_packFileService.FindFile("vfx\\fx.xml", _output)!.DataSource.ReadData(), Is.EqualTo(document.Save()));
        }

        [Test]
        public void A_material_and_a_broken_file_open_with_a_reason_and_no_document()
        {
            _session.Load(File("vfx\\materials\\m.xml"));
            Assert.That(_session.Document, Is.Null);
            Assert.That(_session.LoadError, Does.Contain("not an effect"));
            _session.Load(File("vfx\\broken.xml"));
            Assert.That(_session.Document, Is.Null);
            Assert.That(_session.LoadError, Does.Contain("line 1"));
        }

        [Test]
        public void The_effect_name_and_a_targets_emitters_come_from_the_packs()
        {
            _session.Load(File("vfx\\fx.xml"));
            Assert.That(_session.EffectName, Is.EqualTo("fx"));
            Assert.That(_session.EmitterIdsOf("lib"), Is.EqualTo(new[] { "t1", "t2" }));
            Assert.That(_session.EmitterIdsOf("missing"), Is.Null);
        }

        [Test]
        public void A_target_whose_pack_cannot_be_read_has_no_emitter_list()
        {
            var unreadable = new Mock<IDataSource>();
            unreadable.Setup(s => s.ReadData()).Throws(new System.IO.IOException("the pack moved"));
            File("vfx\\lib.xml").DataSource = unreadable.Object;
            Assert.That(_session.EmitterIdsOf("lib"), Is.Null);
        }
    }
}
