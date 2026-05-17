using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ImportFileCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Import File";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType != NodeType.File && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                _logger.Here().Warning($"Import file blocked for CA pack '{CommandLoggingHelper.DescribePack(_selectedNode.FileOwner)}'");
                standardDialogs.ShowDialogBox("Unable to edit CA packfile");
                return;
            }

            var dialogResult = standardDialogs.ShowSystemOpenFileDialog(multiselect: true);
            if (dialogResult.Result)
            {
                var parentPath = _selectedNode.GetFullPath();
                var files = dialogResult.FilePaths;
                _logger.Here().Information($"Importing {files.Count} file(s) into '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var importedFile = new PackFile(fileName, new MemorySource(fileSystemAccess.FileReadAllBytes(file)));
                    var item = new NewPackFileEntry(parentPath, importedFile);
                    packFileService.AddFilesToPack(_selectedNode.FileOwner, [item]);
                }

                _logger.Here().Information($"Imported {files.Count} file(s) into '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
            else
            {
                _logger.Here().Information($"Import file cancelled for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
        }
    }



}
