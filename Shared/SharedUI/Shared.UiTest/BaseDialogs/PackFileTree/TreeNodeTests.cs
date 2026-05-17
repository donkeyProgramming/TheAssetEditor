using Shared.Core.PackFiles.Models.Containers;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    internal class TreeNodeTests
    {
        [Test]
        public void DirectoryWithoutChildren_StartsEmpty()
        {
            var container = new PackFileContainer("test.pack");

            var directoryNode = new TreeNode("documents", NodeType.Directory, container, null);

            Assert.That(directoryNode.Children, Is.Empty,
                "Directories should start empty until the view model adds real child nodes.");
        }
    }
}
