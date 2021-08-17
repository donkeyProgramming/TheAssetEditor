
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFragment;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CommonControls.Table;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace AnimationEditor.Common.ReferenceModel
{
    public class ReferenceModelSelectionViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<ReferenceModelSelectionViewModel>();
        PackFileService _pfs;

        // Header
        string _headerName;
        public string HeaderName { get => _headerName; set => SetAndNotify(ref _headerName, value); }

        string _subHeaderName = "";
        public string SubHeaderName { get => _subHeaderName; set => SetAndNotify(ref _subHeaderName, value); }

        AssetViewModel _data;
        public AssetViewModel Data { get => _data; set => SetAndNotify(ref _data, value); }

        public SelectMeshViewModel MeshViewModel { get; set; }
        public SelectAnimationViewModel AnimViewModel { get; set; }
        public SkeletonPreviewViewModel SkeletonInformation { get; set; }
        public SelectMetaViewModel MetaFileInformation { get; set; }
        public SelectFragAndSlotViewModel FragAndSlotSelection { get; set; }



        // Visability
        bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                SetAndNotify(ref _isVisible, value);
                Data.ShowMesh.Value = value;
                Data.ShowSkeleton.Value = value;
            }
        }

        public NotifyAttr<bool> IsControlVisible { get; set; } = new NotifyAttr<bool>(true);

        public ReferenceModelSelectionViewModel(PackFileService pf, AssetViewModel data, string headerName, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _pfs = pf;
            HeaderName = headerName;
            Data = data;

            MeshViewModel = new SelectMeshViewModel(_pfs, Data);
            AnimViewModel = new SelectAnimationViewModel(Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            MetaFileInformation = new SelectMetaViewModel(Data, _pfs);
            FragAndSlotSelection = new SelectFragAndSlotViewModel(_pfs, skeletonAnimationLookUpHelper, Data, MetaFileInformation);

            // Data.PropertyChanged += Data_PropertyChanged;
            Data.AnimationChanged += Data_AnimationChanged;
            Data.SkeletonChanged += Data_SkeletonChanged;
        }

        private void Data_SkeletonChanged(GameSkeleton newValue)
        {
            Data_PropertyChanged(null, null);
        }

        private void Data_AnimationChanged(AnimationClip newValue)
        {
            Data_PropertyChanged(null, null);
        }

        private void Data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SubHeaderName = "";

            if (Data.Skeleton != null)
                SubHeaderName = Data.Skeleton.SkeletonName;

            if (Data.AnimationClip != null)
                SubHeaderName += " - " + Data.AnimationName.Value;
        }

        public void BrowseMesh()
        {
            MeshViewModel.BrowseMesh();
        }

        public void ViewFragment()
        {
            if (FragAndSlotSelection.FragmentList.SelectedItem != null)
            {
                var view = AnimationFragmentViewModel.CreateFromFragment(_pfs, FragAndSlotSelection.FragmentList.SelectedItem, false);
                TableWindow.Show(view);
            }
        }

        public void ViewMetaData() { }
        public void ViewPersistMetaData() { }
    }
}
