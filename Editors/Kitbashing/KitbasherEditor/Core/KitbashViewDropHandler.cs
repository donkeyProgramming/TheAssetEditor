using System.IO;
using Editors.KitbasherEditor.UiCommands;
using Shared.Core.Events;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Editors.KitbasherEditor.ViewModels
{
    public class KitbashViewDropHandler
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public KitbashViewDropHandler(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
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
            _uiCommandFactory.Create<ImportReferenceMeshCommand>().Execute(node.Item);
            return true;
        }
    }
}
