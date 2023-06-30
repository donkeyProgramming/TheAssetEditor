using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using Serilog;
using View3D.Animation;
using View3D.Animation.MetaData;

namespace AnimationEditor.Common.ReferenceModel
{
    public class ReferenceModelSelectionViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<ReferenceModelSelectionViewModel>();
        PackFileService _pfs;
        IComponentManager _componentManager;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        IToolFactory _toolFactory;
        ApplicationSettingsService _applicationSettingsService;

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
        public NotifyAttr<bool> AllowMetaData { get; set; } = new NotifyAttr<bool>(false);
        public ReferenceModelSelectionViewModel(IToolFactory toolFactory, PackFileService pf, AssetViewModel data, string headerName, IComponentManager componentManager, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, ApplicationSettingsService applicationSettingsService)
        {
            _toolFactory = toolFactory;
            _pfs = pf;
            HeaderName = headerName;
            _componentManager = componentManager;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            Data = data;
            _applicationSettingsService = applicationSettingsService;

            MeshViewModel = new SelectMeshViewModel(_pfs, Data);
            AnimViewModel = new SelectAnimationViewModel(Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            MetaFileInformation = new SelectMetaViewModel(Data, _pfs);
            FragAndSlotSelection = new SelectFragAndSlotViewModel(_pfs, skeletonAnimationLookUpHelper, Data, MetaFileInformation, applicationSettingsService);

            Data.AnimationChanged += Data_AnimationChanged;
            Data.SkeletonChanged += Data_SkeletonChanged;
            Data.MetaDataChanged += MetaDataChanged;
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
            FragAndSlotSelection.PreviewSelectedSlot();
        }

        public void ViewSelectedMeta()
        {
            var fullFileName = _pfs.GetFullPath(_data.MetaData);
            var viewModel = _toolFactory.Create(fullFileName);
            viewModel.MainFile = _data.MetaData;
            var window = _toolFactory.CreateAsWindow(viewModel);
            window.Show();
        }

        public void ViewSelectedPersistMeta()
        {
            var fullFileName = _pfs.GetFullPath(_data.PersistMetaData);
            var viewModel = _toolFactory.Create(fullFileName);
            viewModel.MainFile = _data.PersistMetaData;
            var window = _toolFactory.CreateAsWindow(viewModel);
            window.Width = 800;
            window.Height = 450;
            window.Title = "Persistent meta file - " + fullFileName;

            window.Show();
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

            var fatory = new MetaDataFactory(model.MainNode, _componentManager, model, model.Player, FragAndSlotSelection.FragmentList.SelectedItem, _applicationSettingsService);
            model.MetaDataItems = fatory.Create(persist, meta);
        }

        public void Refresh()
        {
            MetaFileInformation.Refresh();
        }
    }
}
