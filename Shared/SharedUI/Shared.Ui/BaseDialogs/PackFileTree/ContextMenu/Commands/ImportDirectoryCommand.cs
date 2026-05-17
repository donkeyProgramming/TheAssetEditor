using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportDirectoryCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ImportDirectoryCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Import Directory";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType != NodeType.File && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                _logger.Here().Warning($"Import directory blocked for CA pack '{CommandLoggingHelper.DescribePack(_selectedNode.FileOwner)}'");
                standardDialogs.ShowDialogBox("Unable to edit CA packfile");
                return;
            }

            var folderDialogResult = standardDialogs.ShowSystemFolderBrowserDialog();
            if (folderDialogResult.Result && !string.IsNullOrEmpty(folderDialogResult.FolderPath))
            {
                var folderPath = folderDialogResult.FolderPath;
                var folderName = fileSystemAccess.CreateDirectoryInfo(folderPath).Name;
                var originalFilePaths = fileSystemAccess.DirectoryGetFiles(folderPath, "*", SearchOption.AllDirectories);
                var filePaths = originalFilePaths.Select(x => x.Replace($"{folderPath}\\", "")).ToList();

                _logger.Here().Information($"Importing directory '{folderPath}' into '{CommandLoggingHelper.DescribeNode(_selectedNode)}' ({originalFilePaths.Length} file(s))");

                var packNodeParentPath = _selectedNode.GetFullPath();
                if (!string.IsNullOrWhiteSpace(packNodeParentPath))
                    packNodeParentPath += "\\";

                var filesAdded = new List<NewPackFileEntry>();
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var currentPath = filePaths[i];
                    var fileName = fileSystemAccess.PathGetFileName(currentPath);

                    var packDirectoryPath = $"{packNodeParentPath.ToLower()}{folderName}";

                    var directoryPath = string.Empty;
                    if (currentPath != fileName)
                    {
                        directoryPath = currentPath.Replace($"\\{fileName}", string.Empty).ToLower();
                        packDirectoryPath = $"{packDirectoryPath}\\{directoryPath}";
                    }

                    var source = new MemorySource(fileSystemAccess.FileReadAllBytes(originalFilePaths[i]));
                    var file = new PackFile(fileName, source);

                    filesAdded.Add(new NewPackFileEntry(packDirectoryPath, file));
                }

                packFileService.AddFilesToPack(_selectedNode.FileOwner, filesAdded);
                _logger.Here().Information($"Imported {filesAdded.Count} file(s) from '{folderPath}' into '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
            else
            {
                _logger.Here().Information($"Import directory cancelled for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
        }
    }
}
