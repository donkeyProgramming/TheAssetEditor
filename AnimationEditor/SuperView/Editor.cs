using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using AnimationMeta.Presentation;
using AnimationMeta.Visualisation;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimationEditor.SuperView
{
    public class Editor
    {
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly AnimationPlayerViewModel _player;
        private readonly MetaDataFactory _metaDataFactory;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;
        private readonly IToolFactory _toolFactory;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public NotifyAttr<string> PersistentMetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> PersistentMetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<string> MetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> MetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public EditorViewModel PersistentMetaEditor { get; set; }
        public EditorViewModel MetaEditor { get; set; }

        public ObservableCollection<ReferenceModelSelectionViewModel> Items { get; set; } = new ObservableCollection<ReferenceModelSelectionViewModel>();

        public Editor(MetaDataFactory metaDataFactory, AssetViewModelBuilder assetViewModelBuilder, IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, AnimationPlayerViewModel player, CopyPasteManager copyPasteManager, ApplicationSettingsService applicationSettingsService)
        {
            _metaDataFactory = metaDataFactory;
            _assetViewModelBuilder = assetViewModelBuilder;
            _toolFactory = toolFactory;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _player = player;
            _applicationSettingsService = applicationSettingsService;

            PersistentMetaEditor = new EditorViewModel(pfs, copyPasteManager);
            PersistentMetaEditor.EditorSavedEvent += PersistentMetaEditor_EditorSavedEvent;
            MetaEditor = new EditorViewModel(pfs, copyPasteManager);
            MetaEditor.EditorSavedEvent += MetaEditor_EditorSavedEvent;
        }

        public Editor Create(AnimationToolInput input)
        {
            var asset = _assetViewModelBuilder.CreateAsset("Item 0", Color.Black);
            _player.RegisterAsset(asset);
            var viewModel = new ReferenceModelSelectionViewModel(_metaDataFactory, _toolFactory, _pfs, asset, "Item 0:", _assetViewModelBuilder, _skeletonHelper, _applicationSettingsService);
            viewModel.AllowMetaData.Value = true;

            if (input.Mesh != null)
                _assetViewModelBuilder.SetMesh(asset, input.Mesh);

            if (input.Animation != null)
                _assetViewModelBuilder.SetAnimation(viewModel.Data, _skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));

            if (input.FragmentName != null)
            {
                viewModel.FragAndSlotSelection.FragmentList.SelectedItem = viewModel.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == input.FragmentName);

                if (input.AnimationSlot != null)
                    viewModel.FragAndSlotSelection.FragmentSlotList.SelectedItem = viewModel.FragAndSlotSelection.FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == input.AnimationSlot.Value);
            }

            asset.MetaDataChanged += Asset_MetaDataChanged;

            Items.Add(viewModel);
            Asset_MetaDataChanged(asset);
            return this;
        }

        private void Asset_MetaDataChanged(AssetViewModel newValue)
        {
            PersistentMetaEditor.MainFile = newValue.PersistMetaData;
            if (PersistentMetaEditor.MainFile != null)
            {
                PersistentMetaFilePackFileContainerName.Value = _pfs.GetPackFileContainer(PersistentMetaEditor.MainFile).Name;
                PersistentMetaFilePath.Value = _pfs.GetFullPath(PersistentMetaEditor.MainFile);
            }
            else
            {
                PersistentMetaFilePath.Value = "";
                PersistentMetaFilePackFileContainerName.Value = "";
            }

            MetaEditor.MainFile = newValue.MetaData;
            if (MetaEditor.MainFile != null)
            {
                MetaFilePackFileContainerName.Value = _pfs.GetPackFileContainer(MetaEditor.MainFile).Name;
                MetaFilePath.Value = _pfs.GetFullPath(MetaEditor.MainFile);
            }
            else
            {
                MetaFilePath.Value = "";
                MetaFilePackFileContainerName.Value = "";
            }
        }

        private void MetaEditor_EditorSavedEvent(CommonControls.FileTypes.PackFiles.Models.PackFile newFile)
        {
            Items.First().MetaFileInformation.SelectedMetaFile = newFile;
        }

        private void PersistentMetaEditor_EditorSavedEvent(CommonControls.FileTypes.PackFiles.Models.PackFile newFile)
        {
            Items.First().MetaFileInformation.SelectedPersistMetaFile = newFile;
        }

        public void RefreshAction()
        {
            foreach (var item in Items)
                item.Refresh();

            foreach (var item in Items)
                item.Data.TriggerMeshChanged();
        }

    }
}
