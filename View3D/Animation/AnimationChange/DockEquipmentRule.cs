using Microsoft.Xna.Framework;
using View3D.SceneNodes;

namespace View3D.Animation.AnimationChange
{
    public class DockEquipmentRule : AnimationChangeRule
    {
        public enum DockSlot
        {
            LeftHand,
            RightHand
        }

        DockSlot _slot; 
        int _equipmentSlotToDock;
        AnimationClip _dockAnimation;
        ISkeletonProvider _skeletonProvider;

        int _dockTargetkBoneId;

        public DockEquipmentRule(DockSlot slot, int equipmentSlotToDock, AnimationClip dockAnimation, ISkeletonProvider skeletonProvider)
        {
            _slot = slot;
            _equipmentSlotToDock = skeletonProvider.Skeleton.GetBoneIndexByName("be_prop_"+ (equipmentSlotToDock-1));
            _dockAnimation = dockAnimation;
            _skeletonProvider = skeletonProvider;

            if (slot == DockSlot.LeftHand)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("hand_left");
            else if (slot == DockSlot.RightHand)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("hand_right");
            else
                throw new System.NotImplementedException();
        }


        public override void ApplyAfterWorldTransform(AnimationFrame frame)
        {
            var offsetFrame = AnimationSampler.Sample(0, _skeletonProvider.Skeleton, _dockAnimation);
            var offset = offsetFrame.GetSkeletonAnimatedWorldDiff(_skeletonProvider.Skeleton, _dockTargetkBoneId, _equipmentSlotToDock);

            var propTransform = _skeletonProvider.Skeleton.GetAnimatedWorldTranform(_dockTargetkBoneId);
            frame.BoneTransforms[_equipmentSlotToDock].WorldTransform = offset * propTransform;
        }
    }
}
