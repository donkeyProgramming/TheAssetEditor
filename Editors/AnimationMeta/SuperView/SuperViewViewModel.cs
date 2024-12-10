using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.AnimationMeta.Presentation;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.Shared.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles;

namespace Editors.AnimationMeta.SuperView
{
    public partial class SuperViewViewModel : EditorHostBase
    {
        SceneObjectViewModel _asset;

        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly IPackFileService _packFileService;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IEventHub _eventHub;
        private readonly IUiCommandFactory _uiCommandFactory;

        [ObservableProperty] string _persistentMetaFilePath = "";
        [ObservableProperty] string _metaFilePath = "";
        [ObservableProperty] MetaDataEditorViewModel _persistentMetaEditor;
        [ObservableProperty] MetaDataEditorViewModel _metaEditor;

        public override Type EditorViewModelType => typeof(EditorView);

        public SuperViewViewModel(
            IPackFileService packFileService,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            IEventHub eventHub,
            IUiCommandFactory uiCommandFactory,
            SceneObjectEditor sceneObjectBuilder,
            IEditorHostParameters editorHostParameters)
            : base(editorHostParameters)
        {
            DisplayName = "Super view";
            _packFileService = packFileService;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _eventHub = eventHub;
            _uiCommandFactory = uiCommandFactory;
            _sceneObjectBuilder = sceneObjectBuilder;

            Initialize();
            eventHub.Register<ScopedFileSavedEvent>(this, OnFileSaved);
        }

        private void OnFileSaved(ScopedFileSavedEvent evnt)
        {
            var newFile = _packFileService.FindFile(evnt.NewPath);
            if (evnt.FileOwner == PersistentMetaEditor)
                _sceneObjectBuilder.SetMetaFile(_asset.Data, _asset.Data.MetaData, newFile);
            else if (evnt.FileOwner == MetaEditor)
                _sceneObjectBuilder.SetMetaFile(_asset.Data, newFile, _asset.Data.PersistMetaData);
            else
                throw new Exception($"Unable to determine file owner when reciving a file save event in SuperView. Owner:{evnt.FileOwner}, File:{evnt.NewPath}");
        }

        void Initialize()
        {
            PersistentMetaEditor = new MetaDataEditorViewModel(_uiCommandFactory);
            MetaEditor = new MetaDataEditorViewModel(_uiCommandFactory);
            
            var assetViewModel = _sceneObjectViewModelBuilder.CreateAsset(true, "Root", Color.Black,null, true);
            SceneObjects.Add(assetViewModel);
            
            _asset = assetViewModel;
            _asset.Data.MetaDataChanged += UpdateMetaDataInfoFromAsset;
            UpdateMetaDataInfoFromAsset(_asset.Data);
        }

        public void Load(AnimationToolInput debugDataToLoad)
        {
            _sceneObjectBuilder.SetMesh(_asset.Data, debugDataToLoad.Mesh);

            // Hack :(
            if (debugDataToLoad.AnimationSlot != null)
            {
                var frag = _asset.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == debugDataToLoad.FragmentName);
                _asset.FragAndSlotSelection.FragmentList.SelectedItem = frag;

                var slot = _asset.FragAndSlotSelection.FragmentSlotList.PossibleValues.First(x => x.SlotName == debugDataToLoad.AnimationSlot.Value);
                _asset.FragAndSlotSelection.FragmentSlotList.SelectedItem = slot;
            }
        }

        private void UpdateMetaDataInfoFromAsset(SceneObject asset)
        {
            PersistentMetaEditor.LoadFile(asset.PersistMetaData);
            MetaEditor.LoadFile(asset.MetaData);
        }

        public void RefreshAction() => _asset.Data.TriggerMeshChanged();

        public override void Close()
        {
            _asset.Data.MetaDataChanged -= UpdateMetaDataInfoFromAsset;
            _eventHub.UnRegister(this);
            base.Close();
        }
    }
}
