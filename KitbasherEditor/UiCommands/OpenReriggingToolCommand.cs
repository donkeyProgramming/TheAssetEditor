using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using MessageBox = System.Windows.MessageBox;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenReriggingToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the re-rigging tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = null;

        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SelectionManager _selectionManager;
   
        private readonly PackFileService _packFileService;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IWindowFactory _windowFactory;

        public OpenReriggingToolCommand(KitbasherRootScene kitbasherRootScene, SelectionManager selectionManager, PackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper, IWindowFactory windowFactory)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _selectionManager = selectionManager;
    
            _packFileService = packFileService;
            _skeletonHelper = skeletonHelper;
            _windowFactory = windowFactory;
        }
        public void Execute()
        {
            var targetSkeletonName = _kitbasherRootScene.Skeleton.SkeletonName;
            var state = _selectionManager.GetState<ObjectSelectionState>();

            var existingSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, targetSkeletonName);
            if (existingSkeletonFile == null)
                throw new Exception("TargetSkeleton not found -" + targetSkeletonName);

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

            var window = _windowFactory.Create<ReRiggingViewModel, BoneMappingView>("Re-rigging", 1200, 1100);
            window.TypedContext.Initialize(selectedMeshses, window, config);
            window.ShowWindow();
        }
    }
}
