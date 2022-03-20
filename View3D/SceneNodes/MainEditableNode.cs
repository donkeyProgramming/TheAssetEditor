using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using View3D.Services;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode Skeleton { get; private set; }
        public PackFile MainPackFile { get; private set; }
        public RmvVersionEnum SelectedOutputFormat { get; set; }
        public TextureFileEditorService TextureFileEditorService { get; set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, PackFile mainFile, PackFileService pfs) : base(name)
        {
            Skeleton = skeletonNode;
            MainPackFile = mainFile;
            TextureFileEditorService = new TextureFileEditorService(this, pfs);
        }

    }
}
