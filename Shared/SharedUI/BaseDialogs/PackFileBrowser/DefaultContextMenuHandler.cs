using System.Collections.ObjectModel;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{

    public class DefaultContextMenuHandler : ContextMenuHandler
    {
        public DefaultContextMenuHandler(PackFileService service, IUiCommandFactory uiCommandFactory, IExportFileContextMenuHelper exportFileContextMenuHelper) 
            : base(service, uiCommandFactory, exportFileContextMenuHelper)
        {

        }

        public override void Create(TreeNode node)
        {
            _selectedNode = node;
            if (node == null)
            {
                Items = new ObservableCollection<ContextMenuItem>();
                return;
            }

            var newContextMenu = new ObservableCollection<ContextMenuItem>();

            if (node.NodeType == NodeType.Root)
            {
                if (node.FileOwner.IsCaPackFile)
                {
                    Additem(ContextItems.Close, newContextMenu);
                    Additem(ContextItems.Expand, newContextMenu);
                    Additem(ContextItems.Collapse, newContextMenu);
                    AddSeperator(newContextMenu);
                }
                else
                {
                    if (_packFileService.GetEditablePack() != node.FileOwner)
                    {
                        Additem(ContextItems.SetAsEditabelPack, newContextMenu);
                        AddSeperator(newContextMenu);
                    }

                    var addFolder = Additem(ContextItems.Add, newContextMenu);
                    Additem(ContextItems.AddFiles, addFolder);
                    Additem(ContextItems.AddDirectory, addFolder);

                    var createMenu = Additem(ContextItems.Create, newContextMenu);
                    Additem(ContextItems.CreateFolder, createMenu);

                    AddSeperator(newContextMenu);

                    Additem(ContextItems.Expand, newContextMenu);
                    Additem(ContextItems.Collapse, newContextMenu);
                    AddSeperator(newContextMenu);
                    Additem(ContextItems.Save, newContextMenu);
                    Additem(ContextItems.SaveAs, newContextMenu);
                    Additem(ContextItems.Close, newContextMenu);
                }
            }

            if (node.NodeType == NodeType.Directory)
            {
                if (_packFileService.GetEditablePack() != node.FileOwner)
                    Additem(ContextItems.CopyToEditablePack, newContextMenu);
                if (!node.FileOwner.IsCaPackFile)
                {
                    var addFolder = Additem(ContextItems.Add, newContextMenu);
                    Additem(ContextItems.AddFiles, addFolder);
                    Additem(ContextItems.AddDirectory, addFolder);

                    var createMenu = Additem(ContextItems.Create, newContextMenu);
                    Additem(ContextItems.CreateFolder, createMenu);

                    AddSeperator(newContextMenu);

                    AddSeperator(newContextMenu);
                    Additem(ContextItems.Rename, newContextMenu);
                    Additem(ContextItems.Delete, newContextMenu);
                    AddSeperator(newContextMenu);

                }
                Additem(ContextItems.Expand, newContextMenu);
                Additem(ContextItems.Collapse, newContextMenu);
                Additem(ContextItems.ExportToFolder, newContextMenu);
            }

            if (node.NodeType == NodeType.File)
            {
                if (_packFileService.GetEditablePack() != node.FileOwner)
                    Additem(ContextItems.CopyToEditablePack, newContextMenu);
                if (!node.FileOwner.IsCaPackFile)
                {
                    AddSeperator(newContextMenu);
                    Additem(ContextItems.Duplicate, newContextMenu);
                    Additem(ContextItems.Rename, newContextMenu);
                    Additem(ContextItems.Delete, newContextMenu);
                    AddSeperator(newContextMenu);

                }
                Additem(ContextItems.CopyFullPath, newContextMenu);
                Additem(ContextItems.ExportToFolder, newContextMenu);
                if(_exportFileContextMenuHelper.CanExportFile(node.Item))
                    Additem(ContextItems.AdvancedExport, newContextMenu);
                AddSeperator(newContextMenu);

                var openFolder = Additem(ContextItems.Open, newContextMenu);
                Additem(ContextItems.OpenWithHxD, openFolder);
                Additem(ContextItems.OpenWithNodePadPluss, openFolder);
            }

            Items = newContextMenu;
        }
    }
}
