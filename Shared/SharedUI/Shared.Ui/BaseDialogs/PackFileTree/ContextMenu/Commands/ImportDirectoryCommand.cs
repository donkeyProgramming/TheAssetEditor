using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportDirectoryCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<ImportDirectoryCommand>();

        public string GetDisplayName(TreeNode node) => "Import Directory";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType != NodeType.File && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var container = TreeNodeHelper.GetPackFileContainer(_node);
            if (container == null)
            {
                _logger.Here().Warning($"Import directory blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            if (container.IsCaPackFile)
            {
                _logger.Here().Warning($"Import directory blocked for CA pack '{CommandLoggingHelper.DescribePack(container)}'");
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

                _logger.Here().Information($"Importing directory '{folderPath}' into '{CommandLoggingHelper.DescribeNode(_node)}' ({originalFilePaths.Length} file(s))");

                var packNodeParentPath = _node.GetFullPath();
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

                packFileService.AddFilesToPack(container, filesAdded);
                _logger.Here().Information($"Imported {filesAdded.Count} file(s) from '{folderPath}' into '{CommandLoggingHelper.DescribeNode(_node)}'");
            }
            else
            {
                _logger.Here().Information($"Import directory cancelled for '{CommandLoggingHelper.DescribeNode(_node)}'");
            }
        }
    }
}
