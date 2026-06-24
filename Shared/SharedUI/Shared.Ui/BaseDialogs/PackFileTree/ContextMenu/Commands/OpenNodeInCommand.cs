using System.IO;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public abstract class OpenNodeInCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<OpenNodeInCommand>();

        public abstract string GetDisplayName(TreeNode node);
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && TreeNodeHelper.GetPackFile(node) != null;
        public bool IsEnabled(TreeNode node) => true;

        protected TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public abstract void Execute();

        protected void OpenPackFileUsing(string applicationPath, PackFile packFile)
        {
            if (fileSystemAccess.FileExists(applicationPath) == false)
            {
                _logger.Here().Warning($"Unable to open '{packFile.Name}' because application '{applicationPath}' was not found");
                standardDialogs.ShowDialogBox($"Application {applicationPath} does not exist");
                return;
            }

            var tempFolder = Path.GetTempPath();
            var fileName = string.Format(@"{0}_", DateTime.Now.Ticks) + packFile.Name;

            var path = tempFolder + "\\" + fileName;
            var bytes = packFile.DataSource.ReadData();
            fileSystemAccess.FileWriteAllBytes(path, bytes);
            _logger.Here().Information($"Prepared temporary file '{path}' for '{packFile.Name}' ({bytes.Length} byte(s))");

            using var process = fileSystemAccess.ProcessStart(applicationPath, $"\"{path}\"");
            if (process == null)
                _logger.Here().Warning($"Process launch returned null while opening '{packFile.Name}' with '{applicationPath}'");
            else
                _logger.Here().Information($"Opened '{packFile.Name}' with '{applicationPath}'");
        }

        protected string ResolveApplicationPath(string appRelativePath)
        {
            var x64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appRelativePath);
            if (fileSystemAccess.FileExists(x64)) return x64;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), appRelativePath);
        }

        protected void OpenSelectedNodeUsing(TreeNode selectedNode, string applicationPath)
        {
            var packFile = TreeNodeHelper.GetPackFile(selectedNode);
            if (packFile == null)
            {
                _logger.Here().Warning($"Unable to resolve file for node '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected file");
                return;
            }

            OpenPackFileUsing(applicationPath, packFile);
        }
    }

    public class OpenNodeInNotepadCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess, IScopedLogger scopedLogger) : OpenNodeInCommand(standardDialogs, fileSystemAccess, scopedLogger)
    {
        public override string GetDisplayName(TreeNode node) => "Open in Notepad++";
        public override void Execute() => OpenSelectedNodeUsing(_node, ResolveApplicationPath(@"Notepad++\notepad++.exe"));
    }

    public class OpenNodeInHxDCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess, IScopedLogger scopedLogger) : OpenNodeInCommand(standardDialogs, fileSystemAccess, scopedLogger)
    {
        public override string GetDisplayName(TreeNode node) => "Open in Hxd";
        public override void Execute() => OpenSelectedNodeUsing(_node, ResolveApplicationPath(@"HxD\HxD.exe"));
    }
}
