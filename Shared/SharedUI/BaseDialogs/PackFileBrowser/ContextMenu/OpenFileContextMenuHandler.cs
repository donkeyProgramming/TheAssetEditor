using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{/*
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

            if (node.GetNodeType() != NodeType.File)
            {
                Additem(ContextItems.Expand, Items);
                Additem(ContextItems.Collapse, Items);
                AddSeperator(Items);
                Additem(ContextItems.CreateFolder, Items);
            }

        }
    }*/
}
