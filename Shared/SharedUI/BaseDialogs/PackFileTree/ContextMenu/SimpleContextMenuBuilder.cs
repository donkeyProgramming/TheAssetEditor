using Shared.Core.Events;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{
    public class SimpleContextMenuBuilder : ContextMenuBuilder
    {
        public SimpleContextMenuBuilder(IUiCommandFactory commandFactory) : base(ContextMenuType.Simple, commandFactory)
        {
        }

        protected override void Create(ContextMenuItem2 rootNode, TreeNode selectedNode)
        {
            var nodeType = selectedNode.GetNodeType();
            if (nodeType == NodeType.File)
                return;

            Add<ExpandNodeCommand>(selectedNode, rootNode);
            Add<CollapseNodeCommand>(selectedNode, rootNode);
            AddSeperator(rootNode);
            Add<CreateFolderCommand>(selectedNode, rootNode);
        }
    }
}
