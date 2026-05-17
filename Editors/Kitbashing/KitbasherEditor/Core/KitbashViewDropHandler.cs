using System.IO;
using Editors.KitbasherEditor.UiCommands;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Editors.KitbasherEditor.ViewModels
{
    public class KitbashViewDropHandler
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IPackFileService _packFileService;

        public KitbashViewDropHandler(IUiCommandFactory uiCommandFactory, IPackFileService packFileService)
        {
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
        }

        public bool AllowDrop(TreeNode node, TreeNode targeNode = null)
        {
            if (node != null && node.NodeType == NodeType.File)
            {
                var extension = Path.GetExtension(node.Name).ToLower();
                if (extension == ".rigid_model_v2" || extension == ".wsmodel" || extension == ".variantmeshdefinition")
                    return true;
            }
            return false;
        }

        public bool Drop(TreeNode node)
        {
            var packFile = _packFileService.FindFile(node.GetFullPath(), node.FileOwner);
            if (packFile == null)
                return false;

            _uiCommandFactory.Create<ImportReferenceMeshCommand>().Execute(packFile);
            return true;
        }
    }
}
