using Common;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels
{
    public class AnimationControllerViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<AnimationControllerViewModel>();
        SceneContainer _scene;
        PackFileService _packFileService;


        string _headerText = "No animation selected";
        public string HeaderText { get { return _headerText; } set { SetAndNotify(ref _headerText, value); } }

        ObservableCollection<PackFile> _animationList = new ObservableCollection<PackFile>();
        public ObservableCollection<PackFile> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }

        List<string> _skeletonList = new List<string>();
        public List<string> SkeletonList { get { return _skeletonList; } set { SetAndNotify(ref _skeletonList, value); } }

        string _selectedSkeleton;
        public string SelectedSkeleton { get { return _selectedSkeleton; } set { SetAndNotify(ref _selectedSkeleton, value); SkeletonChanged(_selectedSkeleton); } }



        string _selectedAnimation;
        public string SelectedAnimation { get { return _selectedAnimation; } set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(_selectedAnimation); } }

        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match((item as PackFile).Name).Success; }; } }



        PackFile Skeleton;
        PackFile Animation;

        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        public AnimationControllerViewModel(SceneContainer scene, PackFileService pf)
        {
            _scene = scene;
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = new SkeletonAnimationLookUpHelper();
            _skeletonAnimationLookUpHelper.FindAllAnimations(_packFileService);

            var allFilesInFolder = _packFileService.FindAllFilesInDirectory("animations\\skeletons");
            SkeletonList = allFilesInFolder.Where(x => Path.GetExtension(x.Name) == ".anim").Select(x=>pf.GetFullPath(x)).ToList();
        }

        public void SetActiveSkeleton(string skeletonName)
        {
            string animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + skeletonName + ".anim";

            SelectedSkeleton = skeletonFilePath;;
            SelectedAnimation = null;
        }

        private void SkeletonChanged(string selectedSkeletonPath)
        {
            // 

            HeaderText = "";
            Skeleton = null;
            AnimationsForCurrentSkeleton.Clear();
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                Skeleton = _packFileService.FindFile(selectedSkeletonPath) as PackFile;
                HeaderText = Skeleton.Name + " - No Animation";
                var anims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(Skeleton.Name));
                foreach (var anim in anims)
                    AnimationsForCurrentSkeleton.Add(anim);
            }

            SelectedAnimation = null;
        }

        private void AnimationChanged(string selectedAnimationPath)
        {
           //HeaderText = SelectedSkeleton.Name + " - No Animation";
           //if (selectedAnimation != null)
           //{
           //    HeaderText = SelectedSkeleton.Name + " - " + selectedAnimation.Name;
           //}
        }
    }
}
