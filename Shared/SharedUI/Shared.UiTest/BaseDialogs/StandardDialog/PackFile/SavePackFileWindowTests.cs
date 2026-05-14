using System.Reflection;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.StandardDialog.PackFile;

namespace Shared.UiTest.BaseDialogs.StandardDialog.PackFile
{
    [TestFixture]
    internal class SavePackFileWindowTests
    {
        [Test]
        public void BuildTargetPath_RootSelection_DoesNotIntroduceLeadingSlash()
        {
            var owner = new PackFileContainer("test.pack")
            {
                SystemFilePath = "test.pack"
            };
            var root = new TreeNode("test.pack", NodeType.Root, owner, null);

            var path = InvokeBuildTargetPath(root, null, "new_file.txt", new Mock<IPackFileService>().Object);

            Assert.That(path, Is.EqualTo("new_file.txt"));
        }

        [Test]
        public void BuildTargetPath_RootLevelFileSelection_UsesRootRelativePath()
        {
            var owner = new PackFileContainer("test.pack")
            {
                SystemFilePath = "test.pack"
            };
            var root = new TreeNode("test.pack", NodeType.Root, owner, null);
            var existingFile = new Shared.Core.PackFiles.Models.PackFile("existing.txt", new MemorySource([1]));
            var fileNode = new TreeNode("existing.txt", NodeType.File, owner, root, existingFile);

            var path = InvokeBuildTargetPath(fileNode, null, "renamed.txt", new Mock<IPackFileService>().Object);

            Assert.That(path, Is.EqualTo("renamed.txt"));
        }

        private static string InvokeBuildTargetPath(TreeNode? selectedNode, Shared.Core.PackFiles.Models.PackFile? selectedFile, string currentFileName, IPackFileService packFileService)
        {
            var method = typeof(SavePackFileWindow).GetMethod("BuildTargetPath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Expected SavePackFileWindow to expose the private path builder helper");

            return (string)method!.Invoke(null, [selectedNode, selectedFile, currentFileName, packFileService])!;
        }
    }
}