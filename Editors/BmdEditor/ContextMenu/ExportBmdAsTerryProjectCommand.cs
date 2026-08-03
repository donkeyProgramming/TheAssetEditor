using System;
using System.IO;
using System.Text;
using Editors.BmdEditor.Exporting;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.Bmd;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Editors.BmdEditor.ContextMenu
{
    public class ExportBmdAsTerryProjectCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IStandardDialogs _standardDialogs = standardDialogs;
        private readonly IFileSystemAccess _fileSystemAccess = fileSystemAccess;

        public string GetDisplayName(TreeNode node) => "Export as Terry project";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && TreeNodeHelper.GetPackFile(node) != null;
        public bool IsEnabled(TreeNode node)
        {
            var packFile = TreeNodeHelper.GetPackFile(node);
            return packFile != null && packFile.Name.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase);
        }

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var packFile = TreeNodeHelper.GetPackFile(_node);
            if (packFile == null)
                return;

            var dialogResult = _standardDialogs.ShowSystemFolderBrowserDialog();
            if (!dialogResult.Result || string.IsNullOrWhiteSpace(dialogResult.FolderPath))
                return;

            DirectoryHelper.EnsureCreated(dialogResult.FolderPath);

            var bmdFile = BmdParser.Parse(packFile.DataSource.ReadData());
            var project = BmdTerryProjectWriter.Build(bmdFile, BmdReferenceResolver.Create(_packFileService));

            var baseName = Path.GetFileNameWithoutExtension(packFile.Name);
            var terryPath = Path.Combine(dialogResult.FolderPath, baseName + ".terry");
            var layerPath = Path.Combine(dialogResult.FolderPath, $"{baseName}.{project.LayerEntityId}.layer");

            _fileSystemAccess.FileWriteAllBytes(terryPath, Encoding.UTF8.GetBytes(project.TerryXml));
            _fileSystemAccess.FileWriteAllBytes(layerPath, Encoding.UTF8.GetBytes(project.LayerXml));
        }
    }
}
