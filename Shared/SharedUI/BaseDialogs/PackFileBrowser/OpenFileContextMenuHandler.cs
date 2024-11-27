using System.Collections.ObjectModel;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class OpenFileContextMenuHandler : ContextMenuHandler
    {
        public OpenFileContextMenuHandler(IPackFileService service) : base(service, null, null, null)
        { }

        public override void Create(TreeNode node)
        {
            _selectedNode = node;
            Items = new ObservableCollection<ContextMenuItem>();
            if (node == null)
                return;

            if (node.NodeType != NodeType.File)
            {
                Additem(ContextItems.Expand, Items);
                Additem(ContextItems.Collapse, Items);
                AddSeperator(Items);
                Additem(ContextItems.CreateFolder, Items);
            }
        }
    }
}
