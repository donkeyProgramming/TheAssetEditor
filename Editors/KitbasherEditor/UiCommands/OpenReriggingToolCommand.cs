using System.Windows;
using CommonControls.Editors.BoneMapping.View;
using Editors.KitbasherEditor.ViewModels;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;
using MessageBox = System.Windows.MessageBox;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenReriggingToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the re-rigging tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = null;

        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SelectionManager _selectionManager;

        private readonly IPackFileService _packFileService;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IWindowFactory _windowFactory;

        public OpenReriggingToolCommand(KitbasherRootScene kitbasherRootScene, SelectionManager selectionManager, IPackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper, IWindowFactory windowFactory)
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

            var existingSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(targetSkeletonName);
            if (existingSkeletonFile == null)
                throw new Exception("TargetSkeleton not found -" + targetSkeletonName);

            var selectedMeshes = state.SelectedObjects<Rmv2MeshNode>();
            if (selectedMeshes.Count(x => x.Geometry.VertexFormat == UiVertexFormat.Static) != 0)
            {
                MessageBox.Show($"A static mesh is selected, which can not be remapped");
                return;
            }

            var selectedMeshSkeletons = selectedMeshes
                .Select(x => x.Geometry.SkeletonName)
                .Distinct();

            if (selectedMeshSkeletons.Count() != 1)
            {
                MessageBox.Show($"{selectedMeshSkeletons.Count()} skeleton types selected, the tool only works when a single skeleton types is selected");
                return;
            }

            var selectedMeshSkeleton = selectedMeshSkeletons.First();
            var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(selectedMeshSkeleton);

            // Ensure all the bones have valid stuff
            var allUsedBoneIndexes = new List<byte>();
            foreach (var mesh in selectedMeshes)
            {
                var boneIndexes = mesh.Geometry.GetUniqeBlendIndices();
                var activeBonesMin = boneIndexes.Min(x => x);
                var activeBonesMax = boneIndexes.Max(x => x);

                var skeletonBonesMax = newSkeletonFile.Bones.Max(x => x.Id);
                var hasValidBoneMapping = activeBonesMin >= 0 && skeletonBonesMax >= activeBonesMax;
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
            window.TypedContext.Initialize(selectedMeshes, window, config);
            window.ShowWindow();
        }
    }
}
