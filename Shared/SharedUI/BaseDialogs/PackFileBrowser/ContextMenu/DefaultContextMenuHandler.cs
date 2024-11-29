using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{/*
    public class DefaultContextMenuHandler : ContextMenuHandler
    {
        public DefaultContextMenuHandler(IPackFileService service,
            IUiCommandFactory uiCommandFactory,
            IExportFileContextMenuHelper exportFileContextMenuHelper,
            IImportFileContextMenuHelper importtFileContextMenuHelper)
            : base(service, uiCommandFactory, exportFileContextMenuHelper, importtFileContextMenuHelper)
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


           ///////-
           /////
           //var placeholderRoot = new ContextMenuItem2("", null);
           //
           //Add<OnRenameNodeCommand>(node, placeholderRoot);
           //
           //
           //foreach(var item in placeholderRoot.ContextMenu)
           //    newContextMenu.Add(item);
           //
           //////--




            if (node.GetNodeType() == NodeType.Root)
                CreateForRoot(node, newContextMenu);

            if (node.GetNodeType() == NodeType.Directory)
                CreateForDirectory(node, newContextMenu);

            if (node.GetNodeType() == NodeType.File)
                CreateForFile(node, newContextMenu);

            Items = newContextMenu;
        }

        private void CreateForFile(TreeNode node, ObservableCollection<ContextMenuItem> newContextMenu)
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
            if (_exportFileContextMenuHelper.CanExportFile(node.Item))
                Additem(ContextItems.AdvancedExport, newContextMenu);
            AddSeperator(newContextMenu);

            var openFolder = Additem(ContextItems.Open, newContextMenu);
            Additem(ContextItems.OpenWithHxD, openFolder);
            Additem(ContextItems.OpenWithNodePadPluss, openFolder);
        }

        private void CreateForDirectory(TreeNode node, ObservableCollection<ContextMenuItem> newContextMenu)
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
                var importSubMenu = Additem(ContextItems.Import, newContextMenu);
                Additem(ContextItems.AdvancedImport, importSubMenu);

                AddSeperator(newContextMenu);
                Additem(ContextItems.Rename, newContextMenu);
                Additem(ContextItems.Delete, newContextMenu);
                AddSeperator(newContextMenu);

            }
            Additem(ContextItems.Expand, newContextMenu);
            Additem(ContextItems.Collapse, newContextMenu);
            Additem(ContextItems.ExportToFolder, newContextMenu);
        }

        private void CreateForRoot(TreeNode node, ObservableCollection<ContextMenuItem> newContextMenu)
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
    }*/
}
