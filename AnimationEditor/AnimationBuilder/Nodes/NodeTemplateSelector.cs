using CommonControls.Common;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using View3D.Animation;
using static CommonControls.FilterDialog.FilterUserControl;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.AnimationBuilder.Nodes
{

    public class NodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AnimationNodeTemplate { get; set; }
        public DataTemplate AnimationEditorTemplate { get; set; }
        public DataTemplate SeperatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is AnimationNode)
                return AnimationNodeTemplate;
            else if (item is AnimationEditorItem)
                return AnimationEditorTemplate;

            throw new Exception("??");

            //var button = (MenuBarButton)item;
            //if (button.IsSeperator)
            //    return SeperatorTemplate;
            //switch (button)
            //{
            //    case MenuBarGroupButton _:
            //        return RadioButtonTemplate;
            //    default:
            //        return DefaultButtonTemplate;
            //}
        }
    }

    public class AnimationNodeFactory
    {
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        string _activeSkeleton;
        public AnimationNodeFactory(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, string activeSkeleton)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _activeSkeleton = activeSkeleton;
        }

        public AnimationEditorItem CreateAddAnimationNode(string animationName = "")
        {
            var node = new InsertAnimationNode()
            {
                Name = new NotifyAttr<string>("Add Animation"),
                AnimationsForCurrentSkeleton = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(_activeSkeleton),
            };

            if (string.IsNullOrWhiteSpace(animationName) == false)
                node.SelectedAnimation = node.AnimationsForCurrentSkeleton.FirstOrDefault(x => string.Compare(x.AnimationFile, animationName, true) == 0);

            return node;
        }

        public AnimationEditorItem CreateLoopAnimationNode() => null;
        public AnimationEditorItem CreateSpliceAnimationNode()
        {
            return null;
        }
    }


    public class InsertAnimationNode : AnimationEditorItem
    {
        public ObservableCollection<AnimationReference> AnimationsForCurrentSkeleton { get; set; } = new ObservableCollection<AnimationReference>();
        AnimationReference _selectedAnimation;
        public AnimationReference SelectedAnimation { get => _selectedAnimation; set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(value); } }

        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        

        void AnimationChanged(AnimationReference animation)
        { 
        
        }

        //public bool Apply(AnimationClip clip, out string errorText)
        //{ 
        //}
    }

    public class SpliceAnimationNode : AnimationEditorItem
    {
        public ObservableCollection<AnimationReference> AnimationsForCurrentSkeleton { get; set; } = new ObservableCollection<AnimationReference>();
        AnimationReference _selectedAnimation;
        public AnimationReference SelectedAnimation { get => _selectedAnimation; set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(value); } }

        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        uint StartFrame;
        uint NumFrameToApply;
        uint TotalFrames;
        uint BlendInFrames;
        uint BlendOutFrames;

        uint BoneToApply;
        uint DepthToApply;

        void AnimationChanged(AnimationReference animation)
        {

        }
    }

    public class LoopAnimationNode : AnimationEditorItem
    {

    }

    public class SetFrameCountAnimationNode : AnimationEditorItem
    {

    }

    public class DeleteFramesAnimationNode : AnimationEditorItem
    {
        // Start / stop
    }

    public class RotateBoneAnimationNode : AnimationEditorItem
    {
        // transform from origo
        // num rotations
        // Start / stop
    }

}

//Root :
// Start output frames
// actuall output frame count


// Parent
// Is output = filename isfilane internal name
// Is building block = compute order


// AddAnimation - start frame, num frames
// AddRefAnimation - start frame, num frames
// Splice - start time, num frames, bones
// Delete frames - start time, num frames
// TransformBone - BoneId, start time, end time, transform
// Loop
// Set frames - Resample
// Transform bone rotate