using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode Skeleton { get; private set; }
        public PackFile MainPackFile { get; private set; }
        public RmvVersionEnum SelectedOutputFormat { get; set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, PackFile mainFile) : base(name)
        {
            Skeleton = skeletonNode;
            MainPackFile = mainFile;
        }

    }
}
