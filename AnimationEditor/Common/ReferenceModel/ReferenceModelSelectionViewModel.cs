
using Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
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
        public SelectSkeletonAndAnimViewModel AnimViewModel { get; set; }
        public SkeletonPreviewViewModel SkeletonInformation { get; set; }

        // Visability
        bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                SetAndNotify(ref _isVisible, value);
                Data.ShowMesh = value;
                Data.IsSkeletonVisible = value;
                Data.IsAnimationActive = value;
            }
        }

        public ReferenceModelSelectionViewModel(PackFileService pf, AssetViewModel data, string headerName, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _pfs = pf;
            HeaderName = headerName;

            Data = data;
            MeshViewModel = new SelectMeshViewModel(_pfs, Data);
            AnimViewModel = new SelectSkeletonAndAnimViewModel(Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);

            Data.PropertyChanged += Data_PropertyChanged;
        }

        private void Data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SubHeaderName = "";
            if (Data.MeshName != null)
                SubHeaderName = Data.MeshName;

            if (Data.Skeleton != null)
                SubHeaderName += " Skeleton - " + Data.Skeleton.SkeletonName;
        }

        public void BrowseMesh()
        {
            MeshViewModel.BrowseMesh();
        }
    }
}
