using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Services;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public partial class SceneObjectViewModel : ObservableObject
    {
        private readonly IPackFileService _pfs;
        private readonly IStandardDialogs _uiProvider;
        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;
        private readonly IMetaDataFactory _metaDataFactory;

        [ObservableProperty] string _headerName;
        [ObservableProperty] string _subHeaderName;
        [ObservableProperty] SceneObject _data;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] bool _isControlVisible = true;
        [ObservableProperty] bool _allowMetaData = true;
        [ObservableProperty] bool _isEnabled = true;
        [ObservableProperty] bool _isExpand = true;

        public SkeletonPreviewViewModel SkeletonInformation { get; set; }
        public BinAnimationViewModel FragAndSlotSelection { get; set; }

        public SceneObjectViewModel(
            IUiCommandFactory uiCommandFactory,
            IMetaDataFactory metaDataFactory,
            IPackFileService packFileService,
            IStandardDialogs uiProvider,
            SceneObject data,
            string headerName,
            SceneObjectEditor sceneObjectBuilder,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _metaDataFactory = metaDataFactory;
            _pfs = packFileService;
            _uiProvider = uiProvider;
            _sceneObjectBuilder = sceneObjectBuilder;
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
            Data = data;
            HeaderName = headerName;

            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            FragAndSlotSelection = new BinAnimationViewModel(sceneObjectBuilder, _pfs, skeletonAnimationLookUpHelper, Data, uiCommandFactory);

            Data.AnimationChanged += (x) => OnSceneObjectChanged();
            Data.SkeletonChanged += (x) => OnSceneObjectChanged();
            Data.MetaDataChanged += RecreateMetaDataInformation;
        }

        partial void OnIsVisibleChanged(bool value)
        {
            Data.ShowMesh.Value = value;
            Data.ShowSkeleton.Value = value;
        }

        public void ViewFragment() => FragAndSlotSelection.PreviewSelectedSlot();

        private void OnSceneObjectChanged()
        {
            SubHeaderName = "";

            if (Data.Skeleton != null)
                SubHeaderName = Data.Skeleton.SkeletonName;

            if (Data.AnimationClip != null)
                SubHeaderName += " - " + Data.AnimationName.Value;
        }

        public void BrowseMesh()
        {
            var result = _uiProvider.DisplayBrowseDialog([".variantmeshdefinition", ".wsmodel", ".rigid_model_v2"]);
            if (result.Result == true && result.File != null)
            {
                var file = result.File;
                _sceneObjectBuilder.SetMesh(Data, file);
            }
        }

        // Move this to superveiw! 
        void RecreateMetaDataInformation(SceneObject model)
        {
            if (AllowMetaData == false)
                return;

            foreach (var item in model.MetaDataItems)
                item.CleanUp();
            model.MetaDataItems.Clear();
            model.Player.AnimationRules.Clear();

            var parser = new MetaDataFileParser();
            var persist = parser.ParseFile(model.PersistMetaData, _metaDataTagDeSerializer);
            var meta = parser.ParseFile(model.MetaData, _metaDataTagDeSerializer);
            model.MetaDataItems = _metaDataFactory.Create(persist, meta, model.MainNode, model, model.Player, FragAndSlotSelection.FragmentList.SelectedItem);
        }
    }
}
