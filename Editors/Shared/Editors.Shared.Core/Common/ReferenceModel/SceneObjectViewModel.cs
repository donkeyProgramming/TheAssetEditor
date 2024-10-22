using Editors.Shared.Core.Services;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public class SceneObjectViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _pfs;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IMetaDataFactory _metaDataFactory;

        public NotifyAttr<string> HeaderName { get; set; } = new NotifyAttr<string>();

        public NotifyAttr<string> SubHeaderName { get; set; } = new NotifyAttr<string>();

        SceneObject _data;
        public SceneObject Data { get => _data; set => SetAndNotify(ref _data, value); }

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

        public NotifyAttr<bool> IsControlVisible { get; set; } = new(true);
        public NotifyAttr<bool> AllowMetaData { get; set; } = new();

        public SceneObjectViewModel(IUiCommandFactory uiCommandFactory,
            IMetaDataFactory metaDataFactory,
            PackFileService packFileService,
            SceneObject data,
            string headerName,
            SceneObjectEditor sceneObjectBuilder,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _uiCommandFactory = uiCommandFactory;
            _metaDataFactory = metaDataFactory;
            _pfs = packFileService;
            HeaderName.Value = headerName;
            Data = data;

            MeshViewModel = new SelectMeshViewModel(_pfs, Data, sceneObjectBuilder);
            AnimViewModel = new SelectAnimationViewModel(sceneObjectBuilder, Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            MetaFileInformation = new SelectMetaViewModel(sceneObjectBuilder, Data, _pfs);
            FragAndSlotSelection = new SelectFragAndSlotViewModel(sceneObjectBuilder, _pfs, skeletonAnimationLookUpHelper, Data, MetaFileInformation, uiCommandFactory);

            Data.AnimationChanged += (x) => OnSceneObjectChanged();
            Data.SkeletonChanged += (x) => OnSceneObjectChanged();
            Data.MetaDataChanged += RecreateMetaDataInformation;
        }

        public void BrowseMesh() => MeshViewModel.BrowseMesh();
        public void ViewFragment() => FragAndSlotSelection.PreviewSelectedSlot();
        public void ViewSelectedMeta() => ViewMetaDataFile(_data.MetaData, "Meta file - ");
        public void ViewSelectedPersistMeta() => ViewMetaDataFile(_data.PersistMetaData, "Persistent meta file - ");

        private void OnSceneObjectChanged()
        {
            SubHeaderName.Value = "";

            if (Data.Skeleton != null)
                SubHeaderName.Value = Data.Skeleton.SkeletonName;

            if (Data.AnimationClip != null)
                SubHeaderName.Value += " - " + Data.AnimationName.Value;
        }

        void RecreateMetaDataInformation(SceneObject model)
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
            _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(fullFileName, 800, 450);
        }
    }
}
