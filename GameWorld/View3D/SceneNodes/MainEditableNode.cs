using GameWorld.Core.Services;
using Shared.Core.PackFiles;

namespace GameWorld.Core.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode SkeletonNode { get; private set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, IPackFileService pfs) : base(name)
        {
            SkeletonNode = skeletonNode;
        }
    }
}
