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

        List<PackFile> _skeletonList = new List<PackFile>();
        public List<PackFile> SkeletonList { get { return _skeletonList; } set { SetAndNotify(ref _skeletonList, value); } }

        PackFile _selectedSkeleton;
        public PackFile SelectedSkeleton { get { return _selectedSkeleton; } set { SetAndNotify(ref _selectedSkeleton, value); SkeletonChanged(_selectedSkeleton); } }



        PackFile _selectedAnimation;
        public PackFile SelectedAnimation { get { return _selectedAnimation; } set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(_selectedAnimation); } }



        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        public AnimationControllerViewModel(SceneContainer scene, PackFileService pf)
        {
            _scene = scene;
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = new SkeletonAnimationLookUpHelper();
            _skeletonAnimationLookUpHelper.FindAllAnimations(_packFileService);

            var allFilesInFolder = _packFileService.FindAllFilesInDirectory("animations\\skeletons");
            SkeletonList = allFilesInFolder.Where(x => Path.GetExtension(x.Name) == ".anim").ToList();
        }

        public void SetActiveSkeleton(string skeletonName)
        {
            string animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + skeletonName + ".anim";

            SelectedSkeleton = _packFileService.FindFile(skeletonFilePath) as PackFile;
            SelectedAnimation = null;
        }

        private void SkeletonChanged(PackFile selectedSkeleton)
        {
            AnimationsForCurrentSkeleton.Clear();
            var anims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(selectedSkeleton.Name));
            foreach (var anim in anims)
                AnimationsForCurrentSkeleton.Add(anim);

            SelectedAnimation = null;
        }

        private void AnimationChanged(PackFile selectedAnimation)
        {
           // throw new NotImplementedException();
        }
    }
}
