using CommonControls.BaseDialogs.PackFileBrowser;
using KitbasherEditor.Services;
using System.IO;

namespace KitbasherEditor.ViewModels
{
    public class KitbashViewDropHandler
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;

        public KitbashViewDropHandler(KitbashSceneCreator kitbashSceneCreator)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
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

        public bool Drop(TreeNode node, TreeNode targeNode = null)
        {
            _kitbashSceneCreator.LoadReference(node.Item);
            return true;
        }
    }
}
