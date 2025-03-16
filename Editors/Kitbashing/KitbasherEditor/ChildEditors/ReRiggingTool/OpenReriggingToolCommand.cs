using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using Editors.KitbasherEditor.Core;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.ChildEditors.ReRiggingTool
{
    public class OpenReriggingToolCommand : IScopedKitbasherUiCommand, IDisposable
    {
        public string ToolTip { get; set; } = "Open the re-rigging tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly IStandardDialogs _standardDialogs;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SelectionManager _selectionManager;

        private readonly ISkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IAbstractFormFactory<ReRiggingWindow> _formFactory;

        ReRiggingWindow? _windowHandle;

        public OpenReriggingToolCommand(IStandardDialogs standardDialogs, KitbasherRootScene kitbasherRootScene, SelectionManager selectionManager, ISkeletonAnimationLookUpHelper skeletonHelper, IAbstractFormFactory<ReRiggingWindow> formFactory)
        {
            _standardDialogs = standardDialogs;
            _kitbasherRootScene = kitbasherRootScene;
            _selectionManager = selectionManager;

            _skeletonHelper = skeletonHelper;
            _formFactory = formFactory;

        }
        public void Execute()
        {
            if (_windowHandle != null)
            {
                _windowHandle.BringIntoView();
                return;
            }

            var targetSkeletonName = _kitbasherRootScene.Skeleton.SkeletonName;
            var state = _selectionManager.GetState<ObjectSelectionState>();

            var existingSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(targetSkeletonName);
            if (existingSkeletonFile == null)
                throw new Exception("TargetSkeleton not found -" + targetSkeletonName);

            var selectedMeshes = state.SelectedObjects<Rmv2MeshNode>();
            if (selectedMeshes.Count(x => x.Geometry.VertexFormat == UiVertexFormat.Static) != 0)
            {
                _standardDialogs.ShowDialogBox($"A static mesh is selected, which can not be remapped", "Error");
                return;
            }

            var selectedMeshSkeletons = selectedMeshes
                .Select(x => x.Geometry.SkeletonName)
                .Distinct();

            if (selectedMeshSkeletons.Count() != 1)
            {
                _standardDialogs.ShowDialogBox($"{selectedMeshSkeletons.Count()} skeleton types selected, the tool only works when a single skeleton types is selected", "Error");
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
                    _standardDialogs.ShowDialogBox($"Mesh {mesh.Name} has an invalid bones, this might cause issues. Its a result of an invalid re-rigging most of the time", "Error");
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
                _standardDialogs.ShowDialogBox("Trying to map to and from the same skeleton. This does not really make any sense if you are trying to make the mesh fit an other skeleton.", "Error");

            _windowHandle = _formFactory.Create();
            _windowHandle.ViewModel.Initialize(selectedMeshes, config);
            _windowHandle.Show();
            _windowHandle.Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_windowHandle != null)
                _windowHandle.Closed -= OnWindowClosed;

            _windowHandle = null;
        }

        public void Dispose()
        {
            _windowHandle?.Close();
            _windowHandle = null;
        }
    }
}
