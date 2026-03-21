using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.Core.Services;
using GameWorld.Core.Services;
using GameWorld.Core.Components.Input;
using GameWorld.Core.SceneNodes;

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
        private readonly IUiCommandFactory _uiCommandFactory;

        private readonly IWpfGame _wpfGame;
        private readonly FocusSelectableObjectService _cameraService;
        private readonly IKeyboardComponent _keyboard;
        private readonly IMouseComponent _mouse;
        private SuperViewManipulatorComponent _manipulator;

        private bool _isHandlingDragComplete = false;

        private Dictionary<SceneNode, Matrix> _initialMatrices = new Dictionary<SceneNode, Matrix>();

        [ObservableProperty] string _persistentMetaFilePath = "";
        [ObservableProperty] string _metaFilePath = "";
        [ObservableProperty] MetaDataEditorViewModel _persistentMetaEditor;
        [ObservableProperty] MetaDataEditorViewModel _metaEditor;
        [ObservableProperty] int _selectedTabControllerIndex = 0;

        public override Type EditorViewModelType => typeof(EditorView);

        public bool HasUnsavedChanges
        {
            get { return PersistentMetaEditor.HasUnsavedChanges || MetaEditor.HasUnsavedChanges; }
            set
            {
                PersistentMetaEditor.HasUnsavedChanges = value;
                MetaEditor.HasUnsavedChanges = value;
            }
        }

        public SuperViewViewModel(
            IPackFileService packFileService,
            IEventHub eventHub,
            IUiCommandFactory uiCommandFactory,
            SceneObjectEditor sceneObjectBuilder,
            IEditorHostParameters editorHostParameters,
            MetaDataFileParser metaDataFileParser,
            IMetaDataBuilder metaDataFactory,
            IWpfGame wpfGame,
            FocusSelectableObjectService cameraService,
            IKeyboardComponent keyboard,
            IMouseComponent mouse
            )
            : base(editorHostParameters)
        {
            DisplayName = "Super view";
            _packFileService = packFileService;
            _eventHub = eventHub;
            _uiCommandFactory = uiCommandFactory;
            _sceneObjectBuilder = sceneObjectBuilder;
            _metaDataFileParser = metaDataFileParser;
            _metaDataFactory = metaDataFactory;

            _wpfGame = wpfGame;
            _cameraService = cameraService;
            _keyboard = keyboard;
            _mouse = mouse;

            Initialize();

            eventHub.Register<ScopedFileSavedEvent>(this, OnFileSaved);
            eventHub.Register<SceneObjectUpdateEvent>(this, OnSceneObjectUpdated);
            eventHub.Register<MetaDataAttributeChangedEvent>(this, OnMetaDataAttributeChanged);
            eventHub.Register<SelecteMetaDataAttributeChangedEvent>(this, OnSelectedMetaDataAttributeChanged);
        }

        private void OnSelectedMetaDataAttributeChanged(SelecteMetaDataAttributeChangedEvent @event)
        {
            if (_manipulator != null)
                _manipulator.SelectedAttribute = MetaEditor.SelectedAttribute ?? PersistentMetaEditor.SelectedAttribute;

            try { System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { _wpfGame.GetFocusElement()?.Focus(); }); } catch { }

            if (!_isHandlingDragComplete) RecreateMetaDataInformation();
        }

        void OnMetaDataAttributeChanged(MetaDataAttributeChangedEvent @event)
        {
            if (!_isHandlingDragComplete) RecreateMetaDataInformation();
        }

        void OnMetaDataChanged(SceneObject sceneObject)
        {
            if (!_isHandlingDragComplete) RecreateMetaDataInformation();
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

        private int GetTargetInstanceIndex(ParsedMetadataAttribute targetAttr)
        {
            if (targetAttr == null) return -1;
            int index = 0;

            bool ProcessFile(ParsedMetadataFile file)
            {
                if (file == null) return false;
                bool CheckList<T>(IEnumerable<T> items)
                {
                    foreach (var item in items)
                    {
                        if (ReferenceEquals(item, targetAttr) || item.Equals(targetAttr)) return true;
                        index++;
                    }
                    return false;
                }

                if (CheckList(file.GetItemsOfType<IAnimatedPropMeta>())) return true;
                if (CheckList(file.GetItemsOfType<ImpactPosition_v10>())) return true;
                if (CheckList(file.GetItemsOfType<TargetPos_10>())) return true;
                if (CheckList(file.GetItemsOfType<FirePos_v10>())) return true;
                if (CheckList(file.GetItemsOfType<SplashAttack_v10>())) return true;
                if (CheckList(file.GetItemsOfType<IEffectMeta>())) return true;

                return false;
            }

            if (MetaEditor.ParsedFile == null || MetaEditor.ParsedFile.GetItemsOfType<DisablePersistant_v10>().Count == 0)
            {
                if (ProcessFile(PersistentMetaEditor.ParsedFile)) return index;
            }
            if (ProcessFile(MetaEditor.ParsedFile)) return index;

            return -1;
        }

        void Initialize()
        {
            PersistentMetaEditor = new MetaDataEditorViewModel(_uiCommandFactory, _metaDataFileParser, _eventHub);
            MetaEditor = new MetaDataEditorViewModel(_uiCommandFactory, _metaDataFileParser, _eventHub);

            var assetViewModel = _sceneObjectViewModelBuilder.CreateAsset("SuperViewRoot", true, "Root", Color.Black, null);
            SceneObjects.Add(assetViewModel);

            assetViewModel.Data.MetaDataChanged += OnMetaDataChanged;
            _asset = assetViewModel;
            OnSceneObjectUpdated(new SceneObjectUpdateEvent(_asset.Data, false, false, false, true));

            _manipulator = new SuperViewManipulatorComponent(_wpfGame, _cameraService, _keyboard, _mouse);

            _manipulator.GetSelectedNode = () =>
            {
                if (_manipulator.SelectedAttribute == null) return null;
                int targetIndex = GetTargetInstanceIndex(_manipulator.SelectedAttribute);
                if (targetIndex >= 0 && targetIndex < _asset.Data.MetaDataItems.Count)
                {
                    var inst = _asset.Data.MetaDataItems[targetIndex];
                    var nodeField = inst.GetType().GetField("_node", BindingFlags.NonPublic | BindingFlags.Instance);
                    return nodeField?.GetValue(inst) as SceneNode;
                }
                return null;
            };

            // 【FIX】: Pass the actual bone world matrix to the manipulator to prevent position offset after dragging
            _manipulator.GetBoneWorldMatrix = () =>
            {
                if (_manipulator.SelectedAttribute != null && _asset != null && _asset.Data != null)
                {
                    try
                    {
                        dynamic meta = _manipulator.SelectedAttribute;
                        int boneId = -1;

                        try { boneId = meta.BoneId; } catch { try { boneId = meta.NodeIndex; } catch { } }

                        if (boneId >= 0)
                        {
                            dynamic data = _asset.Data;
                            if (data.Skeleton != null)
                            {
                                return data.Skeleton.GetAnimatedWorldTranform(boneId);
                            }
                        }
                    }
                    catch { }
                }
                return Matrix.Identity;
            };

            _manipulator.OnDragStarted += () =>
            {
                _initialMatrices.Clear();
                foreach (var inst in _asset.Data.MetaDataItems)
                {
                    var nodeField = inst.GetType().GetField("_node", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (nodeField != null && nodeField.GetValue(inst) is SceneNode node)
                        _initialMatrices[node] = node.ModelMatrix;
                }
            };

            _manipulator.OnDragUpdate += (worldDeltaPos, worldDeltaRot) =>
            {
                try
                {
                    var node = _manipulator.SelectedNode;
                    if (node != null && _initialMatrices.TryGetValue(node, out Matrix initialMatrix))
                    {
                        Vector3 pivotWorld = _manipulator.TrueWorldPivot;

                        node.ModelMatrix = initialMatrix *
                                           Matrix.CreateTranslation(-pivotWorld) *
                                           Matrix.CreateFromQuaternion(worldDeltaRot) *
                                           Matrix.CreateTranslation(pivotWorld) *
                                           Matrix.CreateTranslation(worldDeltaPos);
                    }
                }
                catch { }
            };

            _manipulator.OnDragCompleted += () =>
            {
                _isHandlingDragComplete = true;
                HasUnsavedChanges = true;

                var currentActiveEditor = MetaEditor.SelectedAttribute != null ? MetaEditor : PersistentMetaEditor;
                if (currentActiveEditor.SelectedTag != null)
                {
                    int selectedIndex = currentActiveEditor.Tags.IndexOf(currentActiveEditor.SelectedTag);
                    currentActiveEditor.UpdateView();
                    if (selectedIndex >= 0 && selectedIndex < currentActiveEditor.Tags.Count)
                        currentActiveEditor.SelectedTag = currentActiveEditor.Tags[selectedIndex];
                }

                RecreateMetaDataInformation();
                _isHandlingDragComplete = false;
            };

            _wpfGame.AddComponent(_manipulator);
        }

        void RecreateMetaDataInformation()
        {
            foreach (var item in SceneObjects)
            {
                foreach (var t in item.Data.MetaDataItems) t.CleanUp();
                item.Data.MetaDataItems.Clear();
                item.Data.Player.AnimationRules.Clear();
            }

            var persist = PersistentMetaEditor.ParsedFile;
            var meta = MetaEditor.ParsedFile;

            _asset.Data.MetaDataItems = _metaDataFactory.Create(persist, meta, MetaEditor.SelectedAttribute, _asset.Data.MainNode, _asset.Data, _asset.Data.Player, _asset.FragAndSlotSelection.FragmentList.SelectedItem);
            _asset.Data.Player.Refresh();
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
            if (_manipulator != null) _wpfGame.RemoveComponent(_manipulator);
            _eventHub?.UnRegister(this);
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
