using CommonControls.BaseDialogs;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.ViewModels.BmiEditor;
using KitbasherEditor.ViewModels.MeshFitter;
using KitbasherEditor.ViewModels.PinTool;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views.EditorViews;
using KitbasherEditor.Views.EditorViews.VertexDebugger;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using View3D.Animation;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using MessageBox = System.Windows.MessageBox;

namespace _componentManager.ViewModels.MenuBarViews
{
    public class ToolActions : NotifyPropertyChangedImpl
    {
        IComponentManager _componentManager;
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;

        ViewOnlySelectedService _viewOnlySelectedComp;
        private readonly SceneManager _sceneManager;
        private readonly CommandFactory _commandFactory;
        private readonly SubToolWindowCreator _subToolWindowCreator;
        private readonly IServiceProvider _serviceProvider;
        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        WindowKeyboard _keyboard;

        public ToolActions(SubToolWindowCreator subToolWindowCreator, IServiceProvider serviceProvider, ComponentManagerResolver componentManagerResolver, PackFileService packFileService, WindowKeyboard keyboard, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionManager selectionManager, ObjectEditor objectEditor, FaceEditor faceEditor, ViewOnlySelectedService viewOnlySelectedComponent, SceneManager sceneManager, CommandFactory commandFactory)
        {
            _subToolWindowCreator = subToolWindowCreator;
            _serviceProvider = serviceProvider;
            _packFileService = packFileService;
            _componentManager = componentManagerResolver.ComponentManager;
            _skeletonHelper = skeletonAnimationLookUpHelper;
            _keyboard = keyboard;

            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
            _faceEditor = faceEditor;
            _viewOnlySelectedComp = viewOnlySelectedComponent;
            _sceneManager = sceneManager;
            _commandFactory = commandFactory;
        }

        public void DivideSubMesh()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DivideIntoSubmeshes(objectSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, true);
        }

        public void MergeObjects()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
            {
                if (objectSelectionState.CurrentSelection().Count >= 2)
                {
                    if (!_objectEditor.CombineMeshes(objectSelectionState, out var errorList))
                        ErrorListWindow.ShowDialog("Combine Errors", errorList, false);
                }
            }
        }

        public void DubplicateObject()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DuplicateObject(objectSelectionState);
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, false);
        }

        public void DeleteObject()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DeleteObject(objectSelectionState);
            else if (_selectionManager.GetState() is FaceSelectionState faceSelection)
                _faceEditor.DeleteFaces(faceSelection);
        }

        public void ExpandFaceSelection()
        {
            _faceEditor.GrowSelection(_selectionManager.GetState() as FaceSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
        }

        public void GroupItems()
        {
            _objectEditor.GroupItems(_selectionManager.GetState() as ObjectSelectionState);
        }

        public void ReduceMesh()
        {
            var selectedObjects = _selectionManager.GetState() as ObjectSelectionState;
            if (selectedObjects == null || selectedObjects.SelectionCount() == 0)
                return;

            var meshNodes = selectedObjects.SelectedObjects()
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            _objectEditor.ReduceMesh(meshNodes, 0.9f, true);
        }

        public void CopyLod0ToEveryLods()
        {
            var res = MessageBox.Show("Are you sure to copy lod 0 to every lod slots? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lodGenerationService = new LodGenerationService(_objectEditor);

            rootNode.GetLodNodes().ForEach(x =>
            {
                x.LodReductionFactor = 1;
                x.OptimizeLod_Alpha = false;
                x.OptimizeLod_Vertex = false;
            });

            lodGenerationService.CreateLodsForRootNode(rootNode);
        }
        public void CreateLods()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lodGenerationService = new LodGenerationService(_objectEditor);
            lodGenerationService.CreateLodsForRootNode(rootNode);
        }

        public void ConvertFacesToVertex()
        {
            _faceEditor.ConvertSelectionToVertex(_selectionManager.GetState() as FaceSelectionState);
        }

        public void ToggleShowSelection()
        {
            _viewOnlySelectedComp.Toggle();
        }

        public void OpenBmiTool()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            var meshNode = state.GetSingleSelectedObject() as Rmv2MeshNode;

            if (meshNode != null)
            {
                var skeletonName = meshNode.Geometry.ParentSkeletonName;

                var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, skeletonName);
                GameSkeleton skeleton = new GameSkeleton(newSkeletonFile, null);

                var window = new ControllerHostWindow(true, ResizeMode.CanResize)
                {
                    DataContext = new BmiViewModel(skeleton, meshNode, _commandFactory),
                    Title = "Bmi Tool",
                    Content = new BmiView(),
                };

                window.Show();
            }
        }

        public void OpenSkeletonReshaperTool()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            MeshFitterViewModel.ShowView(state.CurrentSelection(), _componentManager, _skeletonHelper, _packFileService, _commandFactory);
        }

        public void CreateStaticMeshes()
        {
            // Get the frame
            var animationPlayers = _componentManager.GetComponent<AnimationsContainerComponent>();
            var mainPlayer = animationPlayers.Get("MainPlayer");

            var frame = mainPlayer.GetCurrentAnimationFrame();
            if (frame == null)
            {
                MessageBox.Show("An animation must be playing for this tool to work");
                return;
            }

            var state = _selectionManager.GetState<ObjectSelectionState>();
            var selectedObjects = state.SelectedObjects();
            List<Rmv2MeshNode> meshes = new List<Rmv2MeshNode>();

            GroupNode groupNodeContainer = new GroupNode("staticMesh");
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0 = root.GetLodNodes()[0];
            lod0.AddObject(groupNodeContainer);
            foreach (var obj in selectedObjects)
            {
                if (obj is Rmv2MeshNode meshNode)
                {
                    var cpy = SceneNodeHelper.CloneNode(meshNode);
                    groupNodeContainer.AddObject(cpy);
                    meshes.Add(cpy);
                }
            }

            _commandFactory.Create<CreateAnimatedMeshPoseCommand>()
                .IsUndoable(false)
                .Configure(x => x.Configure(meshes, frame, true))
                .BuildAndExecute();
        }

        public void PinMeshToMesh() => PinToolViewModel.ShowWindow(_selectionManager, _commandFactory);

        public void OpenReRiggingTool()
        {
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var skeletonName = root.SkeletonNode.Name;
            Remap(_selectionManager.GetState<ObjectSelectionState>(), skeletonName);
        }

        public void Remap(ObjectSelectionState state, string targetSkeletonName)
        {
            var existingSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, targetSkeletonName);
            if (existingSkeletonFile == null)
                throw new System.Exception("TargetSkeleton not found -" + targetSkeletonName);

            var selectedMeshses = state.SelectedObjects<Rmv2MeshNode>();
            if (selectedMeshses.Count(x => x.Geometry.VertexFormat == UiVertexFormat.Static) != 0)
            {
                MessageBox.Show($"A static mesh is selected, which can not be remapped");
                return;
            }

            var selectedMeshSkeletons = selectedMeshses
                .Select(x => x.Geometry.ParentSkeletonName)
                .Distinct();

            if (selectedMeshSkeletons.Count() != 1)
            {
                MessageBox.Show($"{selectedMeshSkeletons.Count()} skeleton types selected, the tool only works when a single skeleton types is selected");
                return;
            }

            var selectedMeshSkeleton = selectedMeshSkeletons.First();
            var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, selectedMeshSkeleton);

            // Ensure all the bones have valid stuff
            var allUsedBoneIndexes = new List<byte>();
            foreach (var mesh in selectedMeshses)
            {
                var boneIndexes = mesh.Geometry.GetUniqeBlendIndices();
                var activeBonesMin = boneIndexes.Min(x => x);
                var activeBonesMax = boneIndexes.Max(x => x);

                var skeletonBonesMax = newSkeletonFile.Bones.Max(x => x.Id);
                bool hasValidBoneMapping = activeBonesMin >= 0 && skeletonBonesMax >= activeBonesMax;
                if (!hasValidBoneMapping)
                {
                    MessageBox.Show($"Mesh {mesh.Name} has an invalid bones, this might cause issues. Its a result of an invalid re-rigging most of the time");
                    return;
                }
                allUsedBoneIndexes.AddRange(boneIndexes);
            }

            var animatedBoneIndexes = allUsedBoneIndexes
                .Distinct()
                .Select(x => new AnimatedBone(x, newSkeletonFile.Bones[x].Name))
                .OrderBy(x => x.BoneIndex.Value).
                ToList();

            var config = new RemappedAnimatedBoneConfiguration
            {
                MeshSkeletonName = selectedMeshSkeleton,
                MeshBones = AnimatedBoneHelper.CreateFromSkeleton(newSkeletonFile, animatedBoneIndexes.Select(x => x.BoneIndex.Value).ToList()),

                ParnetModelSkeletonName = targetSkeletonName,
                ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(existingSkeletonFile)
            };

            if (targetSkeletonName == selectedMeshSkeleton)
                MessageBox.Show("Trying to map to and from the same skeleton. This does not really make any sense if you are trying to make the mesh fit an other skeleton.", "Error", MessageBoxButton.OK);

            var window = new BoneMappingWindow(new BoneMappingViewModel(config), false);
            window.ShowDialog();

            if (window.Result == true)
            {
                var remapping = AnimatedBoneHelper.BuildRemappingList(config.MeshBones.First());
                _commandFactory.Create<RemapBoneIndexesCommand>().Configure(x => x.Configure(selectedMeshses, remapping, config.ParnetModelSkeletonName)).BuildAndExecute();
            }
        }

        internal void UpdateWh2Model_ConvertAdditiveBlending()
        {
            UpdateWh2ModelAndConvert(Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.AdditiveBlending);
        }

        internal void UpdateWh2Model_ConvertComparativeBlending()
        {
            UpdateWh2ModelAndConvert(Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.ComparativeBlending);
        }

        internal void UpdateWh2ModelAndConvert(Rmv2UpdaterService.BaseColourGenerationTechniqueEnum conversionTechnique)
        {
            throw new NotImplementedException();
            //var res = MessageBox.Show("Are you sure you want to update the model? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (res != MessageBoxResult.Yes)
            //    return;
            //
            //var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            //var lods = rootNode.GetLodNodes();
            //var firtLod = lods.First();
            //var meshList = firtLod.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
            //var filename = _packFileService.GetFullPath(rootNode.MainPackFile);
            //
            //var service = new Rmv2UpdaterService(_packFileService, true);
            //service.UpdateWh2Models(filename, meshList, conversionTechnique, out var errorList);
            //
            //ErrorListWindow.ShowDialog("Converter", errorList);
        }
        public void ShowVertexDebugInfo()
        {
            _subToolWindowCreator.CreateComponentWindow<VertexDebuggerView, VertexDebuggerViewModel>("Vertex debuger", 1200, 1100);
        }
    }
}
