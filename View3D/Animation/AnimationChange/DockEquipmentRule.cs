using Microsoft.Xna.Framework;
using View3D.SceneNodes;

namespace View3D.Animation.AnimationChange
{
    public class DockEquipmentRule : AnimationChangeRule
    {
        public enum DockSlot
        {
            LeftHand,
            RightHand,
            LeftWaist,
            RightWaist,
            Back
        }

        DockSlot _slot; 
        int _equipmentSlotToDock;
        AnimationClip _dockAnimation;
        ISkeletonProvider _skeletonProvider;
        float _startTime;
        float _endTime;


        int _dockTargetkBoneId;
        Matrix _offset;

        public DockEquipmentRule(DockSlot slot, int equipmentSlotToDock, AnimationClip dockAnimation, ISkeletonProvider skeletonProvider, float startTime, float endTime)
        {
            _slot = slot;
            _equipmentSlotToDock = skeletonProvider.Skeleton.GetBoneIndexByName("be_prop_"+ (equipmentSlotToDock-1));
            _dockAnimation = dockAnimation;
            _skeletonProvider = skeletonProvider;
            _startTime = startTime;
            _endTime = endTime;
            if (slot == DockSlot.LeftHand)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("hand_left");
            else if (slot == DockSlot.RightHand)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("hand_right");
            else if (slot == DockSlot.LeftWaist)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("root");
            else if (slot == DockSlot.RightWaist)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("root");
            else if (slot == DockSlot.Back)
                _dockTargetkBoneId = _skeletonProvider.Skeleton.GetBoneIndexByName("spine_2");
            else
                throw new System.NotImplementedException();

            var offsetFrame = AnimationSampler.Sample(0, _skeletonProvider.Skeleton, _dockAnimation);
            _offset = offsetFrame.GetSkeletonAnimatedWorldDiff(_skeletonProvider.Skeleton, _equipmentSlotToDock, _dockTargetkBoneId);
        }

        public override void ApplyWorldTransform(AnimationFrame frame, float time)
        {
            if (time >= _startTime)
            {
                var offsetFrame = AnimationSampler.Sample(0, _skeletonProvider.Skeleton, _dockAnimation);
                _offset = offsetFrame.GetSkeletonAnimatedWorldDiff(_skeletonProvider.Skeleton, _dockTargetkBoneId, _equipmentSlotToDock);


                var propTransform = _skeletonProvider.Skeleton.GetAnimatedWorldTranform(_dockTargetkBoneId);
                frame.BoneTransforms[_equipmentSlotToDock].WorldTransform = _offset * propTransform;
            }
        }
    }
}
