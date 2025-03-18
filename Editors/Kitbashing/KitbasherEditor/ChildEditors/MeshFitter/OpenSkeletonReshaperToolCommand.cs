using System.Windows;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    public class OpenSkeletonReshaperToolCommand : IScopedKitbasherUiCommand, IDisposable
    {
        public string ToolTip { get; set; } = "Open the skeleton modelling tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly SelectionManager _selectionManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IAbstractFormFactory<MeshFitterWindow> _formFactory;
        private readonly SceneManager _sceneManager;

        MeshFitterWindow? _windowHandle;

        public OpenSkeletonReshaperToolCommand(SelectionManager selectionManager, ISkeletonAnimationLookUpHelper skeletonHelper, IAbstractFormFactory<MeshFitterWindow> formFactory, SceneManager sceneManager)
        {
            _selectionManager = selectionManager;
            _skeletonHelper = skeletonHelper;
            _formFactory = formFactory;

            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            if (_windowHandle != null)
            {
                _windowHandle.BringIntoView();
                return;
            }

            var meshesToFit = _selectionManager
                .GetState<ObjectSelectionState>()
                .CurrentSelection();

            var meshNodes = meshesToFit
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .Cast<Rmv2MeshNode>()
                .ToList();

            var allSkeltonNames = meshNodes
                .Select(x => x.Geometry.SkeletonName)
                .Distinct();

            if (allSkeltonNames.Count() != 1)
            {
                var commaList = string.Join(",", allSkeltonNames);
                MessageBox.Show($"Unexpected number of skeletons - {commaList}. This tool only works for one skeleton");
                return;
            }

            var currentSkeletonName = allSkeltonNames.First();
            var currentSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(currentSkeletonName);

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x => (int)x)
                .ToList();

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var targetSkeleton = rootNode.SkeletonNode;
            var targetSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(targetSkeleton.Name);

            var config = new RemappedAnimatedBoneConfiguration();
            config.ParnetModelSkeletonName = targetSkeleton.Name;
            config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeletonFile);

            config.MeshSkeletonName = currentSkeletonName;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(currentSkeletonFile, usedBoneIndexes);

            _windowHandle = _formFactory.Create();
            _windowHandle.ViewModel.Initialize(config, meshNodes, targetSkeleton.Skeleton, currentSkeletonFile);
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
