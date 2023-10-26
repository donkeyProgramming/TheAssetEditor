// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using CommonControls.Events.UiCommands;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;

namespace CommonControls.PackFileBrowser
{
    public class DefaultContextMenuHandler : ContextMenuHandler
    {
        public DefaultContextMenuHandler(PackFileService service, IToolFactory toolFactory, IUiCommandFactory uiCommandFactory) : base(service, toolFactory, uiCommandFactory)
        { }

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
                    var importSubMenu = Additem(ContextItems.Import, newContextMenu);
                    Additem(ContextItems.Import3DModel, importSubMenu);
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
                    var importSubMenu = Additem(ContextItems.Import, newContextMenu);
                    Additem(ContextItems.Import3DModel, importSubMenu);
                    AddSeperator(newContextMenu);

                    AddSeperator(newContextMenu);
                    Additem(ContextItems.Rename, newContextMenu);
                    Additem(ContextItems.Delete, newContextMenu);
                    AddSeperator(newContextMenu);

                }
                Additem(ContextItems.Expand, newContextMenu);
                Additem(ContextItems.Collapse, newContextMenu);
                Additem(ContextItems.Export, newContextMenu);
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
                Additem(ContextItems.Export, newContextMenu);
                AddSeperator(newContextMenu);

                var openFolder = Additem(ContextItems.Open, newContextMenu);
                Additem(ContextItems.OpenWithHxD, openFolder);
                Additem(ContextItems.OpenWithNodePadPluss, openFolder);

                // TODO: phazer added here, for testing, maybe not best place

                if (node != null)
                {
                    //FileInfo fi = new FileInfo(node.Item.Name);
                    var fileExtension = Path.GetExtension(node.Item.Name);
                    //if (fi.Extension.ToLower() == "rigid_model_v2")
                    if (fileExtension == ".rigid_model_v2")
                    {
                        AddSeperator(newContextMenu);
                        var menuItem = Additem(ContextItems.ExportGeomtry, newContextMenu);                        
                    }
                }
            }
            Items = newContextMenu;

        }
    }
}
