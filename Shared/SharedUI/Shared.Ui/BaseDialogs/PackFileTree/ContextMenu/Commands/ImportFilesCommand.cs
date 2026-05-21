using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ImportFileCommand>();

        public string GetDisplayName(TreeNode node) => "Import File";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType != NodeType.File && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(_selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Import file blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            if (container.IsCaPackFile)
            {
                _logger.Here().Warning($"Import file blocked for CA pack '{CommandLoggingHelper.DescribePack(container)}'");
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
                    packFileService.AddFilesToPack(container, [item]);
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
