using Shared.Core.PackFiles.Models.Containers;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    internal class TreeNodeTests
    {
        [Test]
        public void DirectoryWithoutLoadedChildren_ShowsPlaceholderForExpandIcon()
        {
            var container = new PackFileContainer("test.pack");

            var directoryNode = new TreeNode("documents", NodeType.Directory, container, null);

            Assert.That(directoryNode.Children.Any(c => c.Name == "<placeholder>"), Is.True,
                "Collapsed directory without loaded children should contain a placeholder so TreeView shows expand icon.");
        }
    }
}
