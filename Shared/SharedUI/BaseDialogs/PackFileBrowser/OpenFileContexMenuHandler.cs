using System.Collections.ObjectModel;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class OpenFileContexMenuHandler : ContextMenuHandler
    {
        public OpenFileContexMenuHandler(PackFileService service) : base(service, null, null)
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
