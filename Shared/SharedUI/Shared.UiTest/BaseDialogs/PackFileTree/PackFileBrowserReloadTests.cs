using System.IO;
using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    // A pack open in the tree is reloaded after another program (RPFM) saved it
    internal class PackFileBrowserReloadTests : PackFileTreeTestBase
    {
        private static readonly GameInformation s_wh3 = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
        private string _dir = null!;
        private string _packPath = null!;

        [SetUp]
        public void CreatePack()
        {
            _dir = Path.Combine(Path.GetTempPath(), "TreeReload_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _packPath = Path.Combine(_dir, "mymod.pack");
            WritePack(1, "a.txt", "b.txt");
        }

        [TearDown]
        public void DeletePack()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        private void WritePack(int version, params string[] names)
        {
            var writer = PackFileContainer.CreatePackFile("mymod", _packPath, PackFileVersion.PFH5);
            foreach (var name in names)
                writer.AddOrUpdateFile(@"folder\" + name, PackFile.CreateFromASCII(name, name + version));
            writer.SaveToDisk(_packPath, false, s_wh3);
            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(version));
        }

        private PackFileContainer LoadPack()
        {
            PackFileContainer pack;
            using (var stream = File.OpenRead(_packPath))
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
                pack = PackFileSerializerLoader.Load(_packPath, stream.Length, reader, new CaPackDuplicateFileResolver());
            pack.TrackChangesOnDisk();
            return pack;
        }

        private static TreeNode? Find(TreeNode root, string folder, string file)
        {
            var folderNode = root.Children.FirstOrDefault(x => x.Name == folder);
            return folderNode?.Children.FirstOrDefault(x => x.Name == file);
        }

        [Test]
        public void Reload_TreeShowsTheNewVersion_OnlyAeEditsMarkedUnsaved_PackKeepsItsPlace()
        {
            AddPackFiles(false, "before", "before.pack", @"x\before.txt");
            var pack = LoadPack();
            _packFileService.AddContainer(pack);
            AddPackFiles(false, "after", "after.pack", @"x\after.txt");
            _packFileService.SetEditablePack(pack);
            var browser = PackFileBrowser();

            _packFileService.SaveFile(pack.FindFile(@"folder\a.txt")!, Encoding.ASCII.GetBytes("AE EDIT"));
            WritePack(2, "a.txt", "b.txt", "c.txt");
            _packFileService.ReloadPackContainer(pack);

            var root = (RootTreeNode)browser.Files[1];
            Assert.That(root.Owner, Is.SameAs(pack));
            Assert.That(root.IsMainEditabelPack, Is.True);

            var a = Find(root, "folder", "a.txt");
            var b = Find(root, "folder", "b.txt");
            var c = Find(root, "folder", "c.txt");
            Assert.That(new[] { a, b, c }, Has.None.Null);
            Assert.That(root.UnsavedChanges.IsChanged(a!), Is.True);
            Assert.That(root.UnsavedChanges.IsChanged(b!), Is.False);
            Assert.That(root.UnsavedChanges.IsChanged(c!), Is.False);
        }

        [Test]
        public void Reload_NoAeEdits_NothingMarkedUnsaved()
        {
            var pack = LoadPack();
            _packFileService.AddContainer(pack);
            var browser = PackFileBrowser();

            WritePack(2, "a.txt", "c.txt");
            _packFileService.ReloadPackContainer(pack);

            var root = (RootTreeNode)browser.Files.Single();
            Assert.That(root.UnsavedChanges.HasChanges, Is.False);
            Assert.That(Find(root, "folder", "b.txt"), Is.Null);
            Assert.That(Find(root, "folder", "c.txt"), Is.Not.Null);
        }
    }
}
