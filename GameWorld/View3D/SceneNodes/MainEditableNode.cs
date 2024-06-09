using Shared.Core.PackFiles;
using View3D.Services;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode SkeletonNode { get; private set; }
        public TextureFileEditorService TextureFileEditorService { get; set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, PackFileService pfs) : base(name)
        {
            SkeletonNode = skeletonNode;
            TextureFileEditorService = new TextureFileEditorService(this, pfs);
        }
    }
}
