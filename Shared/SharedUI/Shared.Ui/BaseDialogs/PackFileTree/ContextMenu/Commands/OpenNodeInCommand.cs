using System;
using System.Diagnostics;
using System.IO;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public abstract class OpenNodeInCommand(IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public abstract string GetDisplayName(TreeNode node);
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && node.Item != null;
        public bool IsEnabled(TreeNode node) => true;

        public abstract void Execute(TreeNode _selectedNode);

        protected void OpenPackFileUsing(string applicationPath, PackFile packFile)
        {
            if (File.Exists(applicationPath) == false)
            {
                standardDialogs.ShowDialogBox($"Application {applicationPath} does not exist");
                return;
            }

            var tempFolder = Path.GetTempPath();
            var fileName = string.Format(@"{0}_", DateTime.Now.Ticks) + packFile.Name;

            var path = tempFolder + "\\" + fileName;
            var bytes = packFile.DataSource.ReadData();
            File.WriteAllBytes(path, bytes);

            using var process = Process.Start(applicationPath, $"\"{path}\"");
        }

        protected static string ResolveApplicationPath(string appRelativePath)
        {
            var x64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appRelativePath);
            if (File.Exists(x64)) return x64;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), appRelativePath);
        }
    }

    public class OpenNodeInNotepadCommand(IStandardDialogs standardDialogs) : OpenNodeInCommand(standardDialogs)
    {
        public override string GetDisplayName(TreeNode node) => "Open in Notepad++";
        public override void Execute(TreeNode _selectedNode) => OpenPackFileUsing(ResolveApplicationPath(@"Notepad++\notepad++.exe"), _selectedNode.Item!);
    }

    public class OpenNodeInHxDCommand(IStandardDialogs standardDialogs) : OpenNodeInCommand(standardDialogs)
    {
        public override string GetDisplayName(TreeNode node) => "Open in Hxd";
        public override void Execute(TreeNode _selectedNode) => OpenPackFileUsing(ResolveApplicationPath(@"HxD\HxD.exe"), _selectedNode.Item!);
    }
}
