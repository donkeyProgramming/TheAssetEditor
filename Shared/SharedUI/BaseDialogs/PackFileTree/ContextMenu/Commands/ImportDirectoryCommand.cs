using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportDirectoryCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Import Directory";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                System.Windows.MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var folderPath = dialog.SelectedPath;
                var folderName = new DirectoryInfo(folderPath).Name;
                var originalFilePaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                var filePaths = originalFilePaths.Select(x => x.Replace($"{folderPath}\\", "")).ToList();

                var packNodeParentPath = _selectedNode.GetFullPath();
                if (!string.IsNullOrWhiteSpace(packNodeParentPath))
                    packNodeParentPath += "\\";

                var filesAdded = new List<NewPackFileEntry>();
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var currentPath = filePaths[i];
                    var fileName = Path.GetFileName(currentPath);

                    var packDirectoryPath = $"{packNodeParentPath.ToLower()}{folderName}";

                    var directoryPath = string.Empty;
                    if (currentPath != fileName)
                    {
                        directoryPath = currentPath.Replace($"\\{fileName}", string.Empty).ToLower();
                        packDirectoryPath = $"{packDirectoryPath}\\{directoryPath}";
                    }

                    var source = MemorySource.FromFile(originalFilePaths[i]);
                    var file = new PackFile(fileName, source);

                    filesAdded.Add(new NewPackFileEntry(packDirectoryPath, file));
                }

                packFileService.AddFilesToPack(_selectedNode.FileOwner, filesAdded);
            }
        }
    }
}
