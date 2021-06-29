using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MainEditableNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        MainEditableNode _mainNode;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AnimationControllerViewModel _animationControllerViewModel;
        PackFileService _pf;

        public ObservableCollection<string> SkeletonNameList { get; set; }

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); UpdateSkeletonName(); } }
        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        public MainEditableNodeViewModel(MainEditableNode mainNode, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AnimationControllerViewModel animationControllerViewModel, PackFileService pf)
        {
            _mainNode = mainNode;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _animationControllerViewModel = animationControllerViewModel;
            _pf = pf;

            SkeletonNameList = _skeletonAnimationLookUpHelper.SkeletonFileNames;
            if (_mainNode.Model != null)
            {
                SkeletonName = SkeletonNameList.FirstOrDefault(x => x.Contains(_mainNode.Model.Header.SkeletonName));
                UpdateSkeletonName();
            }
        }

        void UpdateSkeletonName()
        {
            string cleanName = "";
            if (!string.IsNullOrWhiteSpace(SkeletonName))
                cleanName = Path.GetFileNameWithoutExtension(SkeletonName);

            if (_mainNode.Model != null)
            {
                var service = new ModelEditorService(_mainNode);
                service.SetSkeletonName(cleanName);
            }

            _animationControllerViewModel.SetActiveSkeleton(cleanName);   
        }

        public void Dispose()
        {
            _skeletonAnimationLookUpHelper = null;
            _mainNode = null;
        }
    }
}
