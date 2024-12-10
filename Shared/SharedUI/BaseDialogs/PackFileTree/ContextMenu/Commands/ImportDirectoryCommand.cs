using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands
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
                var parentPath = _selectedNode.GetFullPath();
                var originalFilePaths = Directory.GetFiles(parentPath, "*", SearchOption.AllDirectories);
                var filePaths = originalFilePaths.Select(x => x.Replace(dialog.SelectedPath + "\\", "")).ToList();
                if (!string.IsNullOrWhiteSpace(parentPath))
                    parentPath += "\\";

                var filesAdded = new List<NewPackFileEntry>();
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var currentPath = filePaths[i];
                    var filename = Path.GetFileName(currentPath);

                    var source = MemorySource.FromFile(originalFilePaths[i]);
                    var file = new PackFile(filename, source);
                    filesAdded.Add(new NewPackFileEntry(parentPath.ToLower(), file));

                }

                packFileService.AddFilesToPack(_selectedNode.FileOwner, filesAdded);
            }
        }
    }



}
