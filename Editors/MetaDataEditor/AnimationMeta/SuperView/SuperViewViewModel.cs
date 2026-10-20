using CommunityToolkit.Mvvm.ComponentModel;
using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView.Visualisation;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView
{
    public partial class SuperViewViewModel : EditorHostBase, ISaveableEditor
    {
        SceneObjectViewModel _asset;

        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly MetaDataFileParser _metaDataFileParser;
        private readonly IMetaDataBuilder _metaDataFactory;
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;
        private readonly IGlobalEventHub _globalEventHub;
        private readonly IUiCommandFactory _uiCommandFactory;

        [ObservableProperty] string _persistentMetaFilePath = "";
        [ObservableProperty] string _metaFilePath = "";
        [ObservableProperty] MetaDataEditorViewModel _persistentMetaEditor;
        [ObservableProperty] MetaDataEditorViewModel _metaEditor;
        [ObservableProperty] int _selectedTabControllerIndex = 0;
        [ObservableProperty] bool _showVfx = true;

        // The battle entity's vfx_filter_id, offered from the ids the metas' EFFECT tags carry: a tag whose id is neither 0 nor
        // this does not play on that entity. Picked, it holds across animations; until then it is the lowest id the metas carry.
        [ObservableProperty] List<int> _vfxFilterIds = [];
        [ObservableProperty] int? _vfxFilterId;
        bool _showingVfxFilter;
        public override Type EditorViewModelType => typeof(EditorView);
        public bool HasUnsavedChanges
        { 
            get 
            {
                return PersistentMetaEditor.HasUnsavedChanges || MetaEditor.HasUnsavedChanges;
            }
            set 
            {
                PersistentMetaEditor.HasUnsavedChanges = value;
                MetaEditor.HasUnsavedChanges = value;
            } 
        }


        public SuperViewViewModel(
            IPackFileService packFileService,
            IEventHub eventHub,
            IGlobalEventHub globalEventHub,
            IUiCommandFactory uiCommandFactory,
            SceneObjectEditor sceneObjectBuilder,
            IEditorHostParameters editorHostParameters,
            MetaDataFileParser metaDataFileParser,
            IMetaDataBuilder metaDataFactory)
            : base(editorHostParameters)
        {
            DisplayName = "Super view";
            _packFileService = packFileService;
            _eventHub = eventHub;
            _globalEventHub = globalEventHub;
            _uiCommandFactory = uiCommandFactory;
            _sceneObjectBuilder = sceneObjectBuilder;
            _metaDataFileParser = metaDataFileParser;
            _metaDataFactory = metaDataFactory;
            Initialize();
            eventHub.Register<ScopedFileSavedEvent>(this, OnFileSaved);
            eventHub.Register<SceneObjectUpdateEvent>(this, OnSceneObjectUpdated);
            eventHub.Register<MetaDataAttributeChangedEvent>(this, OnMetaDataAttributeChanged);
            eventHub.Register<SelecteMetaDataAttributeChangedEvent>(this, OnSelectedMetaDataAttributeChanged);
            globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => OnPackFilesChanged(x.Container, x.AddedFiles));
            globalEventHub.Register<PackFileContainerFilesUpdatedEvent>(this, x => OnPackFilesChanged(x.Container, x.ChangedFiles));
        }

        // A vfx file, a texture, or a model, material or animation a rigid particle may draw changed in a pack (the VFX editor
        // saved, a file was replaced): forget what was loaded from it and rebuild, so the scene shows it without the tab being
        // reopened.
        static readonly string[] s_reloaded = [".dds", ".rigid_model_v2", ".wsmodel", ".material", ".anim"];

        void OnPackFilesChanged(IPackFileContainer container, List<PackFile> files)
        {
            var paths = files.Select(x => _packFileService.GetFullPath(x, container))
                .Where(x => x.StartsWith("vfx", StringComparison.OrdinalIgnoreCase) || s_reloaded.Any(e => x.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (paths.Count == 0)
                return;
            _metaDataFactory.Forget(paths);
            RecreateMetaDataInformation();
        }

        private void OnSelectedMetaDataAttributeChanged(SelecteMetaDataAttributeChangedEvent @event) => RecreateMetaDataInformation();
        void OnMetaDataAttributeChanged(MetaDataAttributeChangedEvent @event) => RecreateMetaDataInformation();
        void OnMetaDataChanged(SceneObject sceneObject) => RecreateMetaDataInformation();

        partial void OnShowVfxChanged(bool value)
        {
            _metaDataFactory.ShowVfx = value;
            RecreateMetaDataInformation();
        }

        partial void OnVfxFilterIdChanged(int? value)
        {
            if (_showingVfxFilter || value == null || value == _metaDataFactory.VfxFilterId)
                return;
            _metaDataFactory.VfxFilterId = value;
            RecreateMetaDataInformation();
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
            PersistentMetaEditor = new MetaDataEditorViewModel(_uiCommandFactory, _metaDataFileParser, _eventHub);
            MetaEditor = new MetaDataEditorViewModel(_uiCommandFactory, _metaDataFileParser, _eventHub);
            
            var assetViewModel = _sceneObjectViewModelBuilder.CreateAsset("SuperViewRoot", true, "Root", Color.Black,null);
            SceneObjects.Add(assetViewModel);

            assetViewModel.Data.MetaDataChanged += OnMetaDataChanged;

            _asset = assetViewModel;
            OnSceneObjectUpdated(new SceneObjectUpdateEvent(_asset.Data, false, false, false, true));
        }

        void RecreateMetaDataInformation()
        {
            // The builder keeps what an unchanged tag built, so only what it no longer returns is cleaned up.
            var old = SceneObjects.SelectMany(x => x.Data.MetaDataItems).ToList();
            foreach (var item in SceneObjects)
            {
                item.Data.MetaDataItems.Clear();
                item.Data.Player.AnimationRules.Clear();
            }

            var persist = PersistentMetaEditor.ParsedFile;
            var meta = MetaEditor.ParsedFile;

            _asset.Data.MetaDataItems = _metaDataFactory.Create(persist, meta, MetaEditor.SelectedAttribute, _asset.Data.MainNode, _asset.Data, _asset.Data.Player, _asset.FragAndSlotSelection.FragmentList.SelectedItem);
            foreach (var t in old.Except(_asset.Data.MetaDataItems))
                t.CleanUp();
            _asset.Data.Player.Refresh();

            var ids = MetaDataBuilder.VfxFilterIds(persist, meta);
            var filter = _metaDataFactory.VfxFilterId ?? ids.FirstOrDefault();
            _showingVfxFilter = true;
            VfxFilterIds = filter == 0 || ids.Contains(filter) ? ids : [.. ids.Append(filter).Order()];
            VfxFilterId = filter;
            _showingVfxFilter = false;
        }

        private void OnSceneObjectUpdated(SceneObjectUpdateEvent e)
        {
            PersistentMetaEditor.LoadFile(e.Owner.PersistMetaData);
            MetaEditor.LoadFile(e.Owner.MetaData);

            RecreateMetaDataInformation();
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


        public void RefreshAction() => _asset.Data.TriggerMeshChanged();

        public override void Close()
        {
            _eventHub?.UnRegister(this);
            _globalEventHub.UnRegister(this);
            base.Close();
        }

        public bool Save()
        {
            var res0 = PersistentMetaEditor.Save();
            var res1 = MetaEditor.Save();
            return res0 && res1;
        }
    }
}
