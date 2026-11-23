using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView.Editing;
using Editors.AnimationMeta.SuperView.Timeline;
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
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.Ui.Common.Timeline;

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
        private readonly LocalizationManager _localizationManager;
        private readonly CombatMetaDataEditSession _editSession;
        private readonly CombatMetaDataGizmoComponent _gizmo;
        private readonly MetaDataMarkerPickerComponent _markerPicker;

        // Which of a splash attack's points the gizmo is on: the one picked in the viewport, the start when picked in the list.
        CombatMetaDataPoint _splashPoint = CombatMetaDataPoint.SplashStart;
        bool _selectingMarker;

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

        // The 3D view's tool: select only (no gizmo), move, or rotate the selected tag.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectTool), nameof(IsMoveTool), nameof(IsRotateTool))]
        MetaDataEditTool _editTool = MetaDataEditTool.Select;
        public bool IsSelectTool { get => EditTool == MetaDataEditTool.Select; set { if (value) EditTool = MetaDataEditTool.Select; } }
        public bool IsMoveTool { get => EditTool == MetaDataEditTool.Move; set { if (value) EditTool = MetaDataEditTool.Move; } }
        public bool IsRotateTool { get => EditTool == MetaDataEditTool.Rotate; set { if (value) EditTool = MetaDataEditTool.Rotate; } }
        public bool CanEditSelectedIn3D => _editSession.CurrentSource != null;
        public bool CanRotateSelectedIn3D => _editSession.CanRotate;
        public bool CanUndoEditIn3D => _editSession.CanUndo;
        public bool CanRedoEditIn3D => _editSession.CanRedo;

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
            IMetaDataBuilder metaDataFactory,
            LocalizationManager localizationManager,
            CombatMetaDataEditSession editSession,
            CombatMetaDataGizmoComponent gizmo,
            MetaDataMarkerPickerComponent markerPicker)
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
            _localizationManager = localizationManager;
            _editSession = editSession;
            _gizmo = gizmo;
            _markerPicker = markerPicker;

            // Added to this editor's world only: registered as game components, they would join every editor's.
            GameWorld!.AddComponent(_gizmo);
            GameWorld.AddComponent(_markerPicker);
            _markerPicker.Connect(GetMarkers, SelectMarker, FocusMarker, ClearMarkerSelection);
            _editSession.ValueChanged += OnEditValueChanged;
            _editSession.HistoryChanged += NotifyEditState;

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

        private void OnSelectedMetaDataAttributeChanged(SelecteMetaDataAttributeChangedEvent @event)
        {
            RecreateMetaDataInformation();
            ShowTimelineSelection();
            if (!_selectingMarker)
                _splashPoint = CombatMetaDataPoint.SplashStart;
            UpdateEditTarget();
        }

        void OnMetaDataAttributeChanged(MetaDataAttributeChangedEvent @event)
        {
            RecreateMetaDataInformation();
            RebuildTimeline();
        }

        void OnMetaDataChanged(SceneObject sceneObject) => RecreateMetaDataInformation();

        partial void OnSelectedTabControllerIndexChanged(int value)
        {
            ShowTimelineSelection();
            UpdateEditTarget();
        }

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
            assetViewModel.Data.AnimationChanged += OnAnimationChanged;

            // New, delete, move and paste rebuild an editor's Tags
            PersistentMetaEditor.Tags.CollectionChanged += OnTagsChanged;
            MetaEditor.Tags.CollectionChanged += OnTagsChanged;
            Player.TimelineItemClicked += OnTimelineItemClicked;

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
            var (persistent, animation) = (PersistentMetaEditor.ParsedFile, MetaEditor.ParsedFile);
            PersistentMetaEditor.LoadFile(e.Owner.PersistMetaData);
            MetaEditor.LoadFile(e.Owner.MetaData);
            // A file read again has new tags: the 3D edits' undo steps point at the old ones.
            if (PersistentMetaEditor.ParsedFile != persistent)
                _editSession.ResetDocument(PersistentMetaEditor);
            if (MetaEditor.ParsedFile != animation)
                _editSession.ResetDocument(MetaEditor);

            RecreateMetaDataInformation();
            RebuildTimeline();
            UpdateEditTarget();
        }

        void OnAnimationChanged(GameWorld.Core.Animation.AnimationClip? clip) => RebuildTimeline();
        void OnTagsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Tags added, deleted, moved or pasted: the document's 3D undo steps may point at tags no longer in it.
            if (ReferenceEquals(sender, PersistentMetaEditor.Tags))
                _editSession.ResetDocument(PersistentMetaEditor);
            else if (ReferenceEquals(sender, MetaEditor.Tags))
                _editSession.ResetDocument(MetaEditor);
            RebuildTimeline();
        }

        // One marker per timed tag in both META files, on the animation player's scrub bar.
        void RebuildTimeline()
        {
            if (_asset == null)
                return;

            var sources = PersistentMetaEditor.Tags.Select(x => new MetaDataInspectionSource(x._input, MetaDataDocumentOwner.Persistent, x.HasError() == null))
                .Concat(MetaEditor.Tags.Select(x => new MetaDataInspectionSource(x._input, MetaDataDocumentOwner.Animation, x.HasError() == null)));
            var index = MetaDataInspectionIndex.Create(sources);

            var clipDuration = _asset.Data.Player.GetAnimationLengthUs() / 1_000_000.0;
            var persistentName = _localizationManager.Get("MetadataEditor.Tabs.Persistent");
            var animationName = _localizationManager.Get("MetadataEditor.Tabs.Animation");
            var wholeAnimation = _localizationManager.Get("SuperView.Timeline.WholeAnimation");

            Player.SelectedTimelineItem = null;
            Player.TimelineItems.Clear();
            foreach (var item in index.Items.Where(x => x.MarkerKind != null))
            {
                var ownerName = item.Owner == MetaDataDocumentOwner.Persistent ? persistentName : animationName;
                Player.TimelineItems.Add(new MetaDataTimelineItem(item, clipDuration, ownerName, wholeAnimation));
            }
            ShowTimelineSelection();
        }

        // Highlights the marker of the tag selected on the open tab
        void ShowTimelineSelection()
        {
            if (Player == null || PersistentMetaEditor == null || MetaEditor == null)
                return;
            var (owner, editor) = SelectedTabControllerIndex == 0
                ? (MetaDataDocumentOwner.Persistent, PersistentMetaEditor)
                : (MetaDataDocumentOwner.Animation, MetaEditor);
            var selected = editor.SelectedAttribute;
            Player.SelectedTimelineItem = selected == null
                ? null
                : Player.TimelineItems.OfType<MetaDataTimelineItem>().FirstOrDefault(x => x.Item.Owner == owner && ReferenceEquals(x.Item.Source, selected));
        }

        // A clicked marker opens its tag: its tab, the tag selected, and the animation at the tag's start
        void OnTimelineItemClicked(ITimelineItem clicked)
        {
            if (clicked is not MetaDataTimelineItem item)
                return;

            var isPersistent = item.Item.Owner == MetaDataDocumentOwner.Persistent;
            var editor = isPersistent ? PersistentMetaEditor : MetaEditor;
            SelectedTabControllerIndex = isPersistent ? 0 : 1;
            var tag = editor.Tags.FirstOrDefault(x => ReferenceEquals(x._input, item.Item.Source));
            if (tag != null)
                editor.SelectedTag = tag;
            Player.SetAnimationTime(item.Start);
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

        MetaDataEditorViewModel CurrentEditor => SelectedTabControllerIndex == 0 ? PersistentMetaEditor : MetaEditor;

        ISpatialMetaDataPreview? FindPreview(ParsedMetadataAttribute? source) =>
            source == null ? null : _asset?.Data.MetaDataItems.OfType<ISpatialMetaDataPreview>().FirstOrDefault(x => ReferenceEquals(x.Source, source));

        // The gizmo goes on the tag selected on the open tab, when the tag is drawn in the scene and has a point to move.
        void UpdateEditTarget()
        {
            if (_editSession == null || PersistentMetaEditor == null || MetaEditor == null)
                return;

            _gizmo.EndDrag();
            var editor = CurrentEditor;
            var source = editor.SelectedAttribute;
            var point = source is SplashAttack_v3 or SplashAttack_v10 ? _splashPoint : CombatMetaDataPoint.Position;
            // Looked up on every call: a rebuild replaces the instance that draws the tag.
            if (FindPreview(source) == null || !_editSession.SetTarget(editor, source!, point, () => FindPreview(source)?.ReferenceWorldTransform ?? Matrix.Identity))
                _editSession.ClearTarget(editor);

            if (EditTool == MetaDataEditTool.Rotate && !_editSession.CanRotate)
                EditTool = MetaDataEditTool.Move;
            UpdateGizmo();
            NotifyEditState();
        }

        partial void OnEditToolChanged(MetaDataEditTool value) => UpdateGizmo();

        void UpdateGizmo()
        {
            _gizmo.TransformMode = EditTool == MetaDataEditTool.Rotate ? CombatMetaDataTransformMode.Rotate : CombatMetaDataTransformMode.Translate;
            _gizmo.IsEnabled = EditTool != MetaDataEditTool.Select && _editSession.CurrentSource != null;
        }

        void NotifyEditState()
        {
            OnPropertyChanged(nameof(CanEditSelectedIn3D));
            OnPropertyChanged(nameof(CanRotateSelectedIn3D));
            OnPropertyChanged(nameof(CanUndoEditIn3D));
            OnPropertyChanged(nameof(CanRedoEditIn3D));
            UndoEditIn3DCommand.NotifyCanExecuteChanged();
            RedoEditIn3DCommand.NotifyCanExecuteChanged();
            FocusSelectedTagCommand.NotifyCanExecuteChanged();
        }

        void OnEditValueChanged(CombatMetaDataEditChange change)
        {
            var editor = change.Owner as MetaDataEditorViewModel;
            if (change.Kind == CombatMetaDataEditChangeKind.Preview)
            {
                // An effect's locator follows its tag on the next frame, and its particles are rebuilt when the drag ends.
                // The line markers are cheap to redraw.
                if (change.Source is not IEffectMeta)
                    RebuildMarkersDuringDrag();
                editor?.RefreshAttributeValues(change.Source, countAsEdit: false);
                return;
            }

            editor?.RefreshAttributeValues(change.Source, countAsEdit: change.Kind != CombatMetaDataEditChangeKind.Cancelled);
            RecreateMetaDataInformation();
            NotifyEditState();
        }

        // A splash attack refuses to draw with its start on its end; mid-drag that step is skipped rather than an error.
        void RebuildMarkersDuringDrag()
        {
            try
            {
                RecreateMetaDataInformation();
            }
            catch (ConstraintException)
            {
            }
        }

        IReadOnlyList<IMetaDataMarkerPreview> GetMarkers() =>
            _asset?.Data.MetaDataItems.OfType<IMetaDataMarkerPreview>().ToList() ?? [];

        // A marker clicked in the viewport: its tag's tab, the tag selected, and the gizmo on the point clicked.
        void SelectMarker(ParsedMetadataAttribute source, MetaDataMarkerPoint point)
        {
            var isPersistent = PersistentMetaEditor.Tags.Any(x => ReferenceEquals(x._input, source));
            var editor = isPersistent ? PersistentMetaEditor : MetaEditor;
            var tag = editor.Tags.FirstOrDefault(x => ReferenceEquals(x._input, source));
            if (tag == null)
                return;

            _splashPoint = point == MetaDataMarkerPoint.SplashEnd ? CombatMetaDataPoint.SplashEnd : CombatMetaDataPoint.SplashStart;
            _selectingMarker = true;
            try
            {
                SelectedTabControllerIndex = isPersistent ? 0 : 1;
                if (editor.SelectedTag == tag)
                    UpdateEditTarget(); // the other end of the same splash attack
                else
                    editor.SelectedTag = tag;
            }
            finally
            {
                _selectingMarker = false;
            }
        }

        void FocusMarker(ParsedMetadataAttribute source)
        {
            if (FindPreview(source) is { } preview)
                FocusService.LookAt(preview.FocusPosition);
        }

        void ClearMarkerSelection() => CurrentEditor.SelectedTag = null;

        [RelayCommand(CanExecute = nameof(CanUndoEditIn3D))] void UndoEditIn3D() => _editSession.Undo();
        [RelayCommand(CanExecute = nameof(CanRedoEditIn3D))] void RedoEditIn3D() => _editSession.Redo();

        bool CanFocusSelectedTag() => FindPreview(CurrentEditor?.SelectedAttribute) != null;
        [RelayCommand(CanExecute = nameof(CanFocusSelectedTag))] void FocusSelectedTag() => FocusMarker(CurrentEditor.SelectedAttribute!);

        public override void Close()
        {
            _markerPicker.Disconnect();
            _gizmo.EndDrag();
            _editSession.ValueChanged -= OnEditValueChanged;
            _editSession.HistoryChanged -= NotifyEditState;
            GameWorld?.RemoveComponent(_markerPicker);
            GameWorld?.RemoveComponent(_gizmo);
            _eventHub?.UnRegister(this);
            Player.TimelineItemClicked -= OnTimelineItemClicked;
            PersistentMetaEditor.Tags.CollectionChanged -= OnTagsChanged;
            MetaEditor.Tags.CollectionChanged -= OnTagsChanged;
            _asset.Data.AnimationChanged -= OnAnimationChanged;
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

    public enum MetaDataEditTool
    {
        Select,
        Move,
        Rotate,
    }
}
