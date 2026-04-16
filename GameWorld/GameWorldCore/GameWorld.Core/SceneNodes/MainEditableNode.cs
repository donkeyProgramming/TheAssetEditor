using System.Collections.Generic;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode SkeletonNode { get; private set; }
        public List<RmvAttachmentPoint> AttachmentPoints { get; private set; } = [];

        public MainEditableNode(string name, SkeletonNode skeletonNode, IPackFileService pfs) : base(name)
        {
            SkeletonNode = skeletonNode;
        }

        public void SetAttachmentPoints(List<RmvAttachmentPoint> attachmentPointList, bool generateFromSkeletonIfEmptyList)
        {
            AttachmentPoints = attachmentPointList;

            if (AttachmentPoints.Count == 0 && generateFromSkeletonIfEmptyList && SkeletonNode != null && SkeletonNode.Skeleton != null)
            {
                var boneNames = SkeletonNode.Skeleton.BoneNames;
                AttachmentPoints = AttachmentPointHelper.CreateFromBoneList(boneNames);
            }
        }
    }
}
