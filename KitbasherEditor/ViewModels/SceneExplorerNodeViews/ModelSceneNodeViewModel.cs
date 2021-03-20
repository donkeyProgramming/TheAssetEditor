using Common;
using Common.ApplicationSettings;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class ModelSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        string _fileName;
        public string FileName { get { return _fileName; } set { SetAndNotify(ref _fileName, value); } }

        string _selectedVersion;
        public string SelectedVersion { get { return _selectedVersion; } set { SetAndNotify(ref _selectedVersion, value); } }

        public List<string> SkeletonNameList { get; set; }

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); UpdateSkeletonName(); } }
        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }



        ObservableCollection<RmvAttachmentPoint> _attachmentPoints;
        public ObservableCollection<RmvAttachmentPoint> AttachmentPoints { get { return _attachmentPoints; } set { SetAndNotify(ref _attachmentPoints, value); } }

        Nullable<RmvAttachmentPoint> _selectedAtachmentPoint;
        public Nullable<RmvAttachmentPoint> SelectedAttachmentPoint { get { return _selectedAtachmentPoint; } set { SetAndNotify(ref _selectedAtachmentPoint, value); IsRemoveAttachmentPointButtonEnabled = _selectedAtachmentPoint != null; } }

        bool _isRemoveAttachmentPointButtonEnabled = false;
        public bool IsRemoveAttachmentPointButtonEnabled { get { return _isRemoveAttachmentPointButtonEnabled; } set { SetAndNotify(ref _isRemoveAttachmentPointButtonEnabled, value); } }

        public ICommand AddAttachmentPointCommand { get; set; }
        public ICommand RemoveAttachmentPointCommand { get; set; }
        public ICommand FixAttachmentPointsCommand { get; set; }

        Rmv2ModelNode _modelNode;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AnimationControllerViewModel _animationControllerViewModel;

        public ModelSceneNodeViewModel(Rmv2ModelNode node, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AnimationControllerViewModel animationControllerViewModel)
        {
            _modelNode = node;

             FileName = node.Model.FileName;
            SelectedVersion = "7";
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _animationControllerViewModel = animationControllerViewModel;

            SkeletonNameList = _skeletonAnimationLookUpHelper.GetAllSkeletonFileNames();
            SkeletonName = SkeletonNameList.FirstOrDefault(x => x.Contains(node.Model.Header.SkeletonName));

            // Find all attachmentpoints
            var attachmentPoints = node.Model.MeshList.SelectMany(x => x.SelectMany(y => y.AttachmentPoints));
            attachmentPoints = attachmentPoints.DistinctBy(x => x.BoneIndex);
            attachmentPoints = attachmentPoints.OrderBy(x => x.BoneIndex);
            AttachmentPoints = new ObservableCollection<RmvAttachmentPoint>(attachmentPoints);

            AddAttachmentPointCommand = new RelayCommand(AddAttachmentPoint);
            RemoveAttachmentPointCommand = new RelayCommand(RemoveAttachmentPoint);
            FixAttachmentPointsCommand = new RelayCommand(FixAttachmentPoints);

            // Ensure all models have this value set
            UpdateSkeletonName();
            UpdateAttachmentPoint();
        }

        void RemoveAttachmentPoint()
        {
            AttachmentPoints.Remove(SelectedAttachmentPoint.Value);
            SelectedAttachmentPoint = null;
        }

        void AddAttachmentPoint()
        { 
        }

        void FixAttachmentPoints()
        {
            ModelEditorService service = new ModelEditorService(_modelNode);
            //service.SetAttachmentPoints (_animationControllerViewModel.Skeleton);
            
        }

        void UpdateSkeletonName()
        {
            string cleanName = "";
            if (!string.IsNullOrWhiteSpace(SkeletonName))
                cleanName = Path.GetFileNameWithoutExtension(SkeletonName);

            ModelEditorService service = new ModelEditorService(_modelNode);
            service.SetSkeletonName(cleanName);

           // _animationControllerViewModel.SetActiveSkeleton(cleanName);
        }

        void UpdateAttachmentPoint()
        {
            ModelEditorService service = new ModelEditorService(_modelNode);
            service.SetAttachmentPoints(AttachmentPoints.ToList());
        }
    }
}
