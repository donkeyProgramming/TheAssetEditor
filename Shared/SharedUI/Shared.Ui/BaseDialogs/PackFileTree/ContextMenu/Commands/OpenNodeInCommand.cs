using System.Diagnostics;
using System;
using System.Windows;
using Shared.Core.PackFiles.Models;
using System.IO;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public abstract class OpenNodeInCommand() : IContextMenuCommand
    {
        public abstract string GetDisplayName(TreeNode node);
        public bool IsEnabled(TreeNode node) => true;

        public abstract void Execute(TreeNode _selectedNode);

        protected void OpenPackFileUsing(string applicationPath, PackFile packFile)
        {
            if (File.Exists(applicationPath) == false)
            {
                MessageBox.Show($"Application {applicationPath} does not exist");
                return;
            }

            var tempFolder = Path.GetTempPath();
            var fileName = string.Format(@"{0}_", DateTime.Now.Ticks) + packFile.Name;

            var path = tempFolder + "\\" + fileName;
            var bytes = packFile.DataSource.ReadData();
            File.WriteAllBytes(path, bytes);

            Process.Start(applicationPath, $"\"{path}\"");
        }
    }

    public class OpenNodeInNotepadCommand() : OpenNodeInCommand
    {
        public override string GetDisplayName(TreeNode node) => "Open in Notepad++";
        public override void Execute(TreeNode _selectedNode) => OpenPackFileUsing(@"C:\Program Files\Notepad++\notepad++.exe", _selectedNode.Item!);
    }

    public class OpenNodeInHxDCommand() : OpenNodeInCommand
    {
        public override string GetDisplayName(TreeNode node) => "Open in Hxd";
        public override void Execute(TreeNode _selectedNode) => OpenPackFileUsing(@"C:\Program Files\HxD\HxD.exe", _selectedNode.Item!);
    }
}
