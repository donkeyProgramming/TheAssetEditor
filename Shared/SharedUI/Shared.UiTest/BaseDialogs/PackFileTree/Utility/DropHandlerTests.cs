using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.Utility
{
    [TestFixture]
    internal class DropHandlerTests : PackFileTreeTestBase
    {
        [Test]
        public void DragFileToNewFolder_MovesFileUnderTargetFolder()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack",
                @"foldera\file1.txt",
                @"folderb\file2.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");
            var file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file1.txt");

            Assert.That(file1, Is.Not.Null);
            Assert.That(folderB, Is.Not.Null);
            Assert.That(DropHandler.AllowDrop(file1!, folderB!, _packFileService), Is.True);

            var result = DropHandler.Drop(file1!, folderB!, _packFileService);

            Assert.That(result, Is.True);
            var movedNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"folderb\file1.txt");
            Assert.That(movedNode, Is.Not.Null);
            var oldNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file1.txt");
            Assert.That(oldNode, Is.Null);

            // Verify the file path is updated in the container
            var movedFile = _packFileService.FindFile(@"folderb\file1.txt");
            Assert.That(movedFile, Is.Not.Null);
            var oldFile = _packFileService.FindFile(@"foldera\file1.txt");
            Assert.That(oldFile, Is.Null);
        }

        [Test]
        public void DragFolderUnderFolder_IsRejected()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack",
                @"foldera\file1.txt",
                @"folderb\file2.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");

            Assert.That(folderA, Is.Not.Null);
            Assert.That(folderB, Is.Not.Null);
            Assert.That(DropHandler.AllowDrop(folderA!, folderB!, _packFileService), Is.False);

            var result = DropHandler.Drop(folderA!, folderB!, _packFileService);

            Assert.That(result, Is.False);

            // Verify the file path is unchanged in the container
            var originalFile = _packFileService.FindFile(@"foldera\file1.txt");
            Assert.That(originalFile, Is.Not.Null);
        }

        [Test]
        public void DragFileInCaPack_IsRejected()
        {
            AddPackFiles(true, "caPack", @"c:\caPack.pack",
                @"foldera\file1.txt",
                @"folderb\file2.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];

            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");
            var file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file1.txt");

            Assert.That(file1, Is.Not.Null);
            Assert.That(folderB, Is.Not.Null);
            Assert.That(DropHandler.AllowDrop(file1!, folderB!, _packFileService), Is.False);
        }
    }
}
