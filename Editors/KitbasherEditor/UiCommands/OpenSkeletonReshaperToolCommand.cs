using System.Windows;
using CommonControls.Editors.BoneMapping.View;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenSkeletonReshaperToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the skeleton modelling tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = null;

        private readonly SelectionManager _selectionManager;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IPackFileService _pfs;
        private readonly IWindowFactory _windowFactory;
        private readonly SceneManager _sceneManager;

        public OpenSkeletonReshaperToolCommand(SelectionManager selectionManager, SkeletonAnimationLookUpHelper skeletonHelper, IPackFileService pfs, IWindowFactory windowFactory, SceneManager sceneManager)
        {
            _selectionManager = selectionManager;
            _skeletonHelper = skeletonHelper;
            _pfs = pfs;
            _windowFactory = windowFactory;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            Create(state.CurrentSelection());
        }

        void Create(List<ISelectable> meshesToFit)
        {
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

            var window = _windowFactory.Create<MeshFitterViewModel, BoneMappingView>("MeshFitter", 1200, 600);
            window.TypedContext.Initialize(window, config, meshNodes, targetSkeleton.Skeleton, currentSkeletonFile);
            window.ShowWindow();
        }
    }
}
