using Shared.Core.Events;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{
    public class NoContextMenuBuilder : ContextMenuBuilder
    {
        public NoContextMenuBuilder(IUiCommandFactory commandFactory) : base(ContextMenuType.None, commandFactory)
        {

        }

        protected override void Create(ContextMenuItem2 rootNode, TreeNode selectedNode) { }
    }
}
