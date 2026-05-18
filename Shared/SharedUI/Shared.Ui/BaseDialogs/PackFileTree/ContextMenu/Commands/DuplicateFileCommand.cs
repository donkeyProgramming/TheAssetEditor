using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DuplicateFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<DuplicateFileCommand>();

        public string GetDisplayName(TreeNode node) => "Duplicate";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var packFile = TreeNodeHelper.GetPackFile(node);
            return node.NodeType == NodeType.File && packFile != null && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var packFile = TreeNodeHelper.GetPackFile(_selectedNode);
            if (packFile == null)
                return;

            _logger.Here().Information($"Duplicating file node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            Execute(packFile);
        }

        public void Execute(PackFile item)
        {
            var fileName = item.Name;
            var extension = "";
            if (Path.HasExtension(item.Name) == true)
            {
                var index = item.Name.IndexOf('.');
                fileName = item.Name.Substring(0, index);
                extension = item.Name.Substring(index);
            }
            var newName = fileName + "_copy" + extension;
            ReadAndSave(newName, item);
        }

        private void ReadAndSave(string newName, PackFile item)
        {
            var editablePack = packFileService.GetEditablePack();
            if (editablePack == null)
            {
                _logger.Here().Warning($"Duplicate requested for '{item.Name}' but no editable pack is selected");
                standardDialogs.ShowDialogBox("No editable pack selected.");
                return;
            }

            var bytes = item.DataSource.ReadData();
            var packFile = new PackFile(newName, new MemorySource(bytes));
            var parentPath = packFileService.GetFullPath(item);
            var path = Path.GetDirectoryName(parentPath);
            var duplicatePath = string.IsNullOrWhiteSpace(path) ? newName : $"{path}\\{newName}";

            var fileEntry = new NewPackFileEntry(path, packFile);
            packFileService.AddFilesToPack(editablePack, [fileEntry]);
            _logger.Here().Information($"Duplicated file '{parentPath}' as '{duplicatePath}' in editable pack '{CommandLoggingHelper.DescribePack(editablePack)}'");
        }
    }
}
