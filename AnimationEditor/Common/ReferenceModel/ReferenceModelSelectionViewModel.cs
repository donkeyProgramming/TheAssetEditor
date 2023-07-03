using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using View3D.Animation;
using View3D.Animation.MetaData;

namespace AnimationEditor.Common.ReferenceModel
{
    public class ReferenceModelSelectionViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _pfs;
        private readonly MetaDataFactory _metaDataFactory;
        private readonly IToolFactory _toolFactory;

        public NotifyAttr<string> HeaderName { get; set; } = new NotifyAttr<string>();

        public NotifyAttr<string> SubHeaderName { get; set; } = new NotifyAttr<string>();

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
        public NotifyAttr<bool> AllowMetaData { get; set; } = new NotifyAttr<bool>(false);

        public ReferenceModelSelectionViewModel(MetaDataFactory metaDataFactory, IToolFactory toolFactory, PackFileService pf, AssetViewModel data, string headerName, AssetViewModelBuilder assetViewModelEditor,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, ApplicationSettingsService applicationSettingsService)
        {
            _metaDataFactory = metaDataFactory;
            _toolFactory = toolFactory;
            _pfs = pf;
            HeaderName.Value = headerName;
            Data = data;

            MeshViewModel = new SelectMeshViewModel(_pfs, Data, assetViewModelEditor);
            AnimViewModel = new SelectAnimationViewModel(assetViewModelEditor, Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            MetaFileInformation = new SelectMetaViewModel(assetViewModelEditor, Data, _pfs);
            FragAndSlotSelection = new SelectFragAndSlotViewModel(assetViewModelEditor, _pfs, skeletonAnimationLookUpHelper, Data, MetaFileInformation, applicationSettingsService);

            Data.AnimationChanged += Data_AnimationChanged;
            Data.SkeletonChanged += Data_SkeletonChanged;
            Data.MetaDataChanged += MetaDataChanged;
        }

        private void Data_SkeletonChanged(GameSkeleton newValue) => OnPropertyChanged();
        private void Data_AnimationChanged(AnimationClip newValue) => OnPropertyChanged();

        public void BrowseMesh() => MeshViewModel.BrowseMesh();
        public void ViewFragment() => FragAndSlotSelection.PreviewSelectedSlot();
        public void ViewSelectedMeta() => ViewMetaDataFile(_data.MetaData, "Meta file - ");
        public void ViewSelectedPersistMeta() => ViewMetaDataFile(_data.PersistMetaData, "Persistent meta file - ");
        public void Refresh() => MetaFileInformation.Refresh();

        private void OnPropertyChanged()
        {
            SubHeaderName.Value = "";

            if (Data.Skeleton != null)
                SubHeaderName.Value = Data.Skeleton.SkeletonName;

            if (Data.AnimationClip != null)
                SubHeaderName.Value += " - " + Data.AnimationName.Value;
        }

        void MetaDataChanged(AssetViewModel model)
        {
            if (AllowMetaData.Value == false)
                return;

            foreach (var item in model.MetaDataItems)
                item.CleanUp();
            model.MetaDataItems.Clear();
            model.Player.AnimationRules.Clear();

            var parser = new MetaDataFileParser();
            var persist = parser.ParseFile(model.PersistMetaData);
            var meta = parser.ParseFile(model.MetaData);
            model.MetaDataItems = _metaDataFactory.Create(persist, meta, model.MainNode, model, model.Player, FragAndSlotSelection.FragmentList.SelectedItem);
        }

        void ViewMetaDataFile(PackFile packFile, string windowTitlePrefix)
        {
            var fullFileName = _pfs.GetFullPath(_data.PersistMetaData);
            var viewModel = _toolFactory.Create(fullFileName);
            viewModel.MainFile = packFile;
            var window = _toolFactory.CreateAsWindow(viewModel);
            window.Width = 800;
            window.Height = 450;
            window.Title = "Persistent meta file - " + fullFileName;

            window.Show();
        }
    }
}
