using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    internal class TreeNodeTests
    {
        [Test]
        public void DirectoryWithoutChildren_StartsEmpty()
        {
            var container = new PackFileContainer("test.pack");

            var directoryNode = new TreeNode("documents", NodeType.Directory, null);

            Assert.That(directoryNode.Children, Is.Empty,
                "Directories should start empty until the view model adds real child nodes.");
        }
    }
}
