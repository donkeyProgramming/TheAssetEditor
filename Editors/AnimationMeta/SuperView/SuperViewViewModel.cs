using System;
using System.Collections.Generic;
using System.Linq;
using Editors.AnimationMeta.Presentation;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.Shared.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.AnimationMeta.SuperView
{
    public class SuperViewViewModel : EditorHostBase
    {
        SceneObjectViewModel _asset;

        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly PackFileService _packFileService;

        public NotifyAttr<string> PersistentMetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> MetaFilePath { get; set; } = new NotifyAttr<string>("");

        public MetaDataEditorViewModel PersistentMetaEditor { get; private set; }
        public MetaDataEditorViewModel MetaEditor { get; private set; }

        public override Type EditorViewModelType => typeof(EditorView);

        public SuperViewViewModel(
            IUiCommandFactory uiCommandFactory,
            PackFileService packFileService,
            SceneObjectEditor sceneObjectBuilder,
            SkeletonAnimationLookUpHelper skeletonHelper,
            IEditorHostParameters editorHostParameters)
            : base(editorHostParameters)
        {
            DisplayName = "Super view";
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
            _sceneObjectBuilder = sceneObjectBuilder;
            _skeletonHelper = skeletonHelper;
        }

        protected override void Initialize(SceneObjectViewModelBuilder builder, IList<SceneObjectViewModel> sceneNodeList)
        {
            PersistentMetaEditor = new MetaDataEditorViewModel(_uiCommandFactory);
           // PersistentMetaEditor.EditorSavedEvent += PersistentMetaEditor_EditorSavedEvent;
            
            MetaEditor = new MetaDataEditorViewModel(_uiCommandFactory);
            //MetaEditor.EditorSavedEvent += MetaEditor_EditorSavedEvent;
            
            var assetViewModel = builder.CreateAsset(true, "Root", Color.Black,null, true);
            sceneNodeList.Add(assetViewModel);
            
            _asset = assetViewModel;
            _asset.Data.MetaDataChanged += UpdateMetaDataInfoFromAsset;
            UpdateMetaDataInfoFromAsset(_asset.Data);
        }

        public void Load(AnimationToolInput debugDataToLoad)
        {
            _sceneObjectBuilder.SetMesh(_asset.Data, debugDataToLoad.Mesh);
            //_sceneObjectBuilder.SetAnimation(_asset.Data, _skeletonHelper.FindAnimationRefFromPackFile(debugDataToLoad.Animation, _packFileService));
            _asset.FragAndSlotSelection.FragmentList.SelectedItem = _asset.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == debugDataToLoad.FragmentName);
            //_asset.FragAndSlotSelection.FragmentSlotList.SelectedItem = _asset.FragAndSlotSelection.FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == debugDataToLoad.AnimationSlot.Value);
        }

        private void UpdateMetaDataInfoFromAsset(SceneObject asset)
        {
            PersistentMetaEditor.LoadFile(asset.PersistMetaData);
            PersistentMetaFilePath.Value = BuildMetaDataName(asset.PersistMetaData);

            MetaEditor.LoadFile(asset.MetaData);
            MetaFilePath.Value = BuildMetaDataName(asset.MetaData);
        }

        string BuildMetaDataName(PackFile file)
        {
            if (file == null)
                return "";

            var containerName = _packFileService.GetPackFileContainer(PersistentMetaEditor.CurrentFile).Name;
            var filePath = PersistentMetaFilePath.Value = _packFileService.GetFullPath(PersistentMetaEditor.CurrentFile);
            return $"[{containerName}]{filePath}";
        }

        private void MetaEditor_EditorSavedEvent(PackFile newFile)
        {
            _sceneObjectBuilder.SetMetaFile(_asset.Data, newFile, _asset.Data.PersistMetaData);
        }

        private void PersistentMetaEditor_EditorSavedEvent(PackFile newFile)
        {
            _sceneObjectBuilder.SetMetaFile(_asset.Data, _asset.Data.MetaData, newFile);
        }

        public void RefreshAction() => _asset.Data.TriggerMeshChanged();
    }
}
