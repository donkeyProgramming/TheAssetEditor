using CommonControls.Services;
using System.Collections.ObjectModel;

namespace CommonControls.PackFileBrowser
{
    public class OpenFileContexMenuHandler : ContextMenuHandler
    {
        public OpenFileContexMenuHandler(PackFileService service) : base(service)
        { }

        public override void Create(TreeNode node)
        {
            _selectedNode = node;
            Items = new ObservableCollection<PackTreeContextMenuItem>();
            if (node == null)
                return;

            if (node.NodeType != NodeType.File)
            {
                Additem(ContextItems.Expand, Items);
                AddSeperator(Items);
                Additem(ContextItems.CreateFolder, Items);
            }
        }
    }
}
