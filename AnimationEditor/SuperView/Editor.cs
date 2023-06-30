using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using System.Linq;
using View3D.Scene;

namespace AnimationEditor.SuperView
{
    public class Editor
    {
        MainScene _scene;
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        AnimationPlayerViewModel _player;
        IToolFactory _toolFactory;
        ApplicationSettingsService _applicationSettingsService;

        public NotifyAttr<string> PersistentMetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> PersistentMetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<string> MetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> MetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public CommonControls.Editors.AnimMeta.EditorViewModel PersistentMetaEditor { get; set; }
        public CommonControls.Editors.AnimMeta.EditorViewModel MetaEditor { get; set; }

        public ObservableCollection<ReferenceModelSelectionViewModel> Items { get; set; } = new ObservableCollection<ReferenceModelSelectionViewModel>();

        public Editor(IToolFactory toolFactory, MainScene scene, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, AnimationPlayerViewModel player, CopyPasteManager copyPasteManager, ApplicationSettingsService applicationSettingsService)
        {
            _toolFactory = toolFactory;
            _scene = scene;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _player = player;
            _applicationSettingsService = applicationSettingsService;

            PersistentMetaEditor = new CommonControls.Editors.AnimMeta.EditorViewModel(pfs, copyPasteManager);
            PersistentMetaEditor.EditorSavedEvent += PersistentMetaEditor_EditorSavedEvent;
            MetaEditor = new CommonControls.Editors.AnimMeta.EditorViewModel(pfs, copyPasteManager);
            MetaEditor.EditorSavedEvent += MetaEditor_EditorSavedEvent;
        }

        public void Create(AnimationToolInput input)
        {
            var asset = _scene.AddComponent(new AssetViewModel(_pfs, "Item 0", Color.Black, _scene, _applicationSettingsService));
            _player.RegisterAsset(asset);
            var viewModel = new ReferenceModelSelectionViewModel(_toolFactory, _pfs, asset, "Item 0:", _scene, _skeletonHelper, _applicationSettingsService);
            viewModel.AllowMetaData.Value = true;

            if(input.Mesh != null)
                viewModel.Data.SetMesh(input.Mesh);
            if (input.Animation != null)
                viewModel.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));

            if (input.FragmentName != null)
            {
                viewModel.FragAndSlotSelection.FragmentList.SelectedItem = viewModel.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == input.FragmentName);

                if (input.AnimationSlot != null)
                    viewModel.FragAndSlotSelection.FragmentSlotList.SelectedItem = viewModel.FragAndSlotSelection.FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == input.AnimationSlot.Value);
            }

            asset.MetaDataChanged += Asset_MetaDataChanged;

            Items.Add(viewModel);
            Asset_MetaDataChanged(asset);
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
                item.Data.ReApplyMeta();
        }

    }
}
