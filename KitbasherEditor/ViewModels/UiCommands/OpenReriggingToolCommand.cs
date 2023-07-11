using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.Events.UiCommands;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using MessageBox = System.Windows.MessageBox;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenReriggingToolCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        private readonly SceneManager _sceneManager;
        private readonly CommandFactory _commandFactory;
        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonHelper;

        public void Execute()
        {
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var skeletonName = root.SkeletonNode.Name;
            Remap(_selectionManager.GetState<ObjectSelectionState>(), skeletonName);
        }

        public void Remap(ObjectSelectionState state, string targetSkeletonName)
        {
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

            var window = new BoneMappingWindow(new BoneMappingViewModel(config), false);
            window.ShowDialog();

            if (window.Result == true)
            {
                var remapping = AnimatedBoneHelper.BuildRemappingList(config.MeshBones.First());
                _commandFactory.Create<RemapBoneIndexesCommand>().Configure(x => x.Configure(selectedMeshses, remapping, config.ParnetModelSkeletonName)).BuildAndExecute();
            }
        }
    }
}
