using CommonControls.Common;
using CommonControls.Services;
using System.Collections.ObjectModel;

namespace CommonControls.PackFileBrowser
{
    public class DefaultContextMenuHandler : ContextMenuHandler
    {
        public DefaultContextMenuHandler(PackFileService service, IToolFactory toolFactory, IEditorCreator editorCreator) : base(service, toolFactory, editorCreator)
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
                    {
                        var importMenu = Additem(ContextItems.Import, newContextMenu);
                        Additem(ContextItems.Import3dModelFile, importMenu);
                        Additem(ContextItems.ImportAnimation, importMenu);         
                        AddSeperator(newContextMenu);


                    }
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
            //TODO: extrat this mess into several medhods, there is a lot of repating
                if (_packFileService.GetEditablePack() != node.FileOwner)
                    Additem(ContextItems.CopyToEditablePack, newContextMenu);
                if (!node.FileOwner.IsCaPackFile)
                {
                    Additem(ContextItems.Import3dModelFile, newContextMenu);
                    var addFolder = Additem(ContextItems.Add, newContextMenu);
                    Additem(ContextItems.AddFiles, addFolder);                    
                    Additem(ContextItems.AddDirectory, addFolder);

                    var createMenu = Additem(ContextItems.Create, newContextMenu);
                    Additem(ContextItems.CreateFolder, createMenu);

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
            }

            Items = newContextMenu;
        }
    }
}
