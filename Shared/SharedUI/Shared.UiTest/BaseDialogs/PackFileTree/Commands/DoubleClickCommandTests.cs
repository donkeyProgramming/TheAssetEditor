using System.Windows.Input;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.Commands;
using Shared.UiTest.BaseDialogs.PackFileTree.Utility;
using Test.TestingUtility.Shared;

namespace Shared.UiTest.BaseDialogs.PackFileTree.Commands
{
    [TestFixture]
    internal class DoubleClickCommandTests : PackFileTreeTestBase
    {
        private TestKeyboard _keyboard;
        private DoubleClickCommand _command;

        [SetUp]
        public void SetupCommand()
        {
            _keyboard = new TestKeyboard();
            _command = new DoubleClickCommand(_packFileService, _keyboard);
        }

        [Test]
        public void DoubleClickFile_OpensFile()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack", @"foldera\file1.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file1.txt");
            Assert.That(file1, Is.Not.Null);

            PackFile? openedFile = null;
            _command.Execute(file1, null, _ => { }, f => openedFile = f);

            Assert.That(openedFile, Is.Not.Null);
            Assert.That(openedFile!.Name, Is.EqualTo("file1.txt"));
        }

        [Test]
        public void DoubleClickDirectory_TogglesExpansion()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack", @"foldera\file1.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            Assert.That(folderA!.IsNodeExpanded, Is.False);

            _command.Execute(folderA, null, _ => { }, _ => { });

            Assert.That(folderA.IsNodeExpanded, Is.True);

            _command.Execute(folderA, null, _ => { }, _ => { });

            Assert.That(folderA.IsNodeExpanded, Is.False);
        }

        [Test]
        public void DoubleClickDirectory_SetsSelectedItem()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack", @"foldera\file1.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");

            TreeNode? selectedNode = null;
            _command.Execute(folderA, null, n => selectedNode = n, _ => { });

            Assert.That(selectedNode, Is.SameAs(folderA));
        }

        [Test]
        public void CtrlDoubleClickDirectory_ExpandsAllChildren()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack",
                @"foldera\suba\file1.txt",
                @"foldera\subb\file2.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            _keyboard.SetKeyDown(Key.LeftCtrl, true);
            _command.Execute(folderA, root, _ => { }, _ => { });

            var subA = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\suba");
            var subB = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\subb");

            Assert.That(folderA!.IsNodeExpanded, Is.True);
            Assert.That(subA, Is.Not.Null);
            Assert.That(subB, Is.Not.Null);
            Assert.That(subA!.IsNodeExpanded, Is.True);
            Assert.That(subB!.IsNodeExpanded, Is.True);
        }

        [Test]
        public void CtrlDoubleClickDirectory_OnlyCountsVisibleNodes()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack",
                @"foldera\file1.txt",
                @"foldera\file2.txt",
                @"foldera\file3.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            // Hide two of the three files
            var file2 = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file2.txt");
            var file3 = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\file3.txt");
            file2!.IsVisible = false;
            file3!.IsVisible = false;

            // Use a command with MaxExpandCount effectively = 2 (only 1 visible file, under threshold)
            _keyboard.SetKeyDown(Key.LeftCtrl, true);
            _command.Execute(folderA, root, _ => { }, _ => { });

            // Should expand because only 1 visible file node (below MaxExpandCount of 200)
            Assert.That(folderA!.IsNodeExpanded, Is.True);
        }

        [Test]
        public void NullNode_WithNullSelectedItem_DoesNothing()
        {
            PackFile? openedFile = null;
            TreeNode? selected = null;

            _command.Execute(null, null, n => selected = n, f => openedFile = f);

            Assert.That(openedFile, Is.Null);
            Assert.That(selected, Is.Null);
        }

        [Test]
        public void CtrlDoubleClickDirectory_DoesNotExpandWhenOverMaxCount()
        {
            AddPackFiles(false, "myPack", @"c:\myPack.pack",
                @"foldera\folderb\file01.txt",
                @"foldera\folderb\file02.txt",
                @"foldera\folderb\file03.txt",
                @"foldera\folderb\file04.txt",
                @"foldera\folderb\file05.txt",
                @"foldera\folderb\file06.txt",
                @"foldera\folderb\file07.txt",
                @"foldera\folderb\file08.txt",
                @"foldera\folderb\file09.txt",
                @"foldera\folderb\file10.txt",
                @"foldera\folderb\file11.txt");

            var browser = PackFileBrowser();
            var root = browser.Files[0];
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, @"foldera\folderb");
            Assert.That(folderA, Is.Not.Null);
            Assert.That(folderB, Is.Not.Null);

            _command.MaxExpandCount = 10;
            _keyboard.SetKeyDown(Key.LeftCtrl, true);
            _command.Execute(folderA, root, _ => { }, _ => { });

            Assert.That(folderA!.IsNodeExpanded, Is.True, "FolderA should toggle expanded on double-click");
            Assert.That(folderB!.IsNodeExpanded, Is.False, "FolderB should NOT expand because file count exceeds MaxExpandCount");
        }
    }
}
