using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{
    public class MainApplicationContextMenuBuilder : ContextMenuBuilder
    {
        private readonly IPackFileService _packFileService;

        public MainApplicationContextMenuBuilder(IPackFileService packFileService, IUiCommandFactory commandFactory) : base(ContextMenuType.MainApplication, commandFactory)
        {
            _packFileService = packFileService;
        }

        protected override void Create(ContextMenuItem2 rootNode, TreeNode selectedNode)
        {
            var nodeType = selectedNode.GetNodeType();
            switch (nodeType)
            {
                case NodeType.File: 
                    CreateForFile(rootNode, selectedNode); 
                    break;
                case NodeType.Root:
                    CreateForDirectory(rootNode, selectedNode);
                    break;
                case NodeType.Directory:
                    CreateForDirectory(rootNode, selectedNode);
                    break;
            }
        }

        void CreateForFile(ContextMenuItem2 rootNode, TreeNode selectedNode)
        {
            if (_packFileService.GetEditablePack() != selectedNode.FileOwner)
                Add<CopyToEditablePackCommand>(selectedNode, rootNode);

            if (!selectedNode.FileOwner.IsCaPackFile)
            {
                AddSeperator(rootNode);
                Add<DuplicateFileCommand>(selectedNode, rootNode);
                Add<OnRenameNodeCommand>(selectedNode, rootNode);
                Add<DeleteNodeCommand>(selectedNode, rootNode);
                AddSeperator(rootNode);
            }

            Add<CopyNodePathCommand>(selectedNode, rootNode);

            var exportFolder = AddChildMenu("Export", rootNode);
            Add<ExportToDirectoryCommand>(selectedNode, exportFolder);
            Add<AdvancedExportCommand>(selectedNode, exportFolder);

            var openFolder = AddChildMenu("Open", rootNode);
            Add<OpenNodeInHxDCommand>(selectedNode, openFolder);
            Add<OpenNodeInNotepadCommand>(selectedNode, openFolder);
        }

        void CreateForDirectory(ContextMenuItem2 rootNode, TreeNode selectedNode)
        {
            if (selectedNode.GetNodeType() == NodeType.Root)
            {
                // Close
                Add<ClosePackContainerFileCommand>(selectedNode, rootNode);
                AddSeperator(rootNode);

                if (!selectedNode.FileOwner.IsCaPackFile)
                {
                    AddSeperator(rootNode);
                    Add<SavePackFileContainerCommand>(selectedNode, rootNode);
                    Add<SaveAsPackFileContainerCommand>(selectedNode, rootNode);
                    AddSeperator(rootNode);
                }
            }

            if (_packFileService.GetEditablePack() != selectedNode.FileOwner)
                 Add<CopyToEditablePackCommand>(selectedNode, rootNode);
            
            if (!selectedNode.FileOwner.IsCaPackFile)
            {
                var importFolder = AddChildMenu("Import", rootNode);
                Add<ImportFileCommand>(selectedNode, importFolder);
                Add<ImportDirectoryCommand>(selectedNode, importFolder);
                Add<AdvancedImportCommand>(selectedNode, importFolder);

                var createMenu = AddChildMenu("Create", rootNode);
                Add<CreateFolderCommand>(selectedNode, createMenu);

                AddSeperator(rootNode);
                Add<OnRenameNodeCommand>(selectedNode, rootNode);
                Add<DeleteNodeCommand>(selectedNode, rootNode);
                AddSeperator(rootNode);
            }

            Add<ExpandNodeCommand>(selectedNode, rootNode);
            Add<CollapseNodeCommand>(selectedNode, rootNode);
            Add<ExportToDirectoryCommand>(selectedNode, rootNode);
        }
    }
}
