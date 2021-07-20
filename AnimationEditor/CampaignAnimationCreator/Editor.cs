using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFragment;
using CommonControls.Services;
using CommonControls.Table;
using FileTypes.AnimationPack;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class Editor : NotifyPropertyChangedImpl
    {
        public FilterCollection<SkeletonBoneNode> ModelBoneList { get; set; } = new FilterCollection<SkeletonBoneNode>(null);

        AssetViewModel _rider;
        PackFileService _pfs;

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, IComponentManager componentManager)
        {
            _pfs = pfs;
            _rider = rider;
            _rider.SkeletonChanged += SkeletonChanged;
            _rider.AnimationChanged += AnimationChanged;

            SkeletonChanged(_rider.Skeleton);
        }



        public void SaveAnimation()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("No animation selected");
                return;
            }

            if (ModelBoneList.SelectedItem == null)
            {
                MessageBox.Show("No root bone selected");
                return;
            }


            // Convert to simple anim
            // Foreach frame
            //      Foreach bone
            //          Copy translation - root trans
            //          Copy rotation - root rot
            //          Clear root trans
            //          Clear root rot

            /*
              Vector3 translationOffset = new Vector3((float)_animationSettings.Translation.X.Value, (float)_animationSettings.Translation.Y.Value, (float)_animationSettings.Translation.Z.Value);
            Vector3 rotationOffset = new Vector3((float)_animationSettings.Rotation.X.Value, (float)_animationSettings.Rotation.Y.Value, (float)_animationSettings.Rotation.Z.Value);
            var rotationOffsetMatrix = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotationOffset.X), MathHelper.ToRadians(rotationOffset.Y), MathHelper.ToRadians(rotationOffset.Z));

            var newRiderAnim = riderAnimation.Clone();
            newRiderAnim.MergeStaticAndDynamicFrames();

            View3D.Animation.AnimationEditor.LoopAnimation(newRiderAnim, (int)_animationSettings.LoopCounter.Value);

            // Resample
            if (_animationSettings.FitAnimation)
                newRiderAnim = View3D.Animation.AnimationEditor.ReSample(_riderSkeleton, newRiderAnim, mountAnimation.DynamicFrames.Count, mountAnimation.PlayTimeInSec);
             */

            //SaveHelper.Save(_pfs, null);
        }

        public void Convert()
        { 
            
        
        }

        private void AnimationChanged(AnimationClip newValue)
        {
            //throw new NotImplementedException();
        }

        private void SkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
                ModelBoneList.UpdatePossibleValues(null);
            else
                ModelBoneList.UpdatePossibleValues(SkeletonHelper.CreateFlatSkeletonList(newValue));
        }
    }

    
}
