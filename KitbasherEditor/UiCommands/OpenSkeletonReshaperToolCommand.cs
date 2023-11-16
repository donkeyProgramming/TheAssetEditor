using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenSkeletonReshaperToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the skeleton modelling tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = null;

        private readonly SelectionManager _selectionManager;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly PackFileService _pfs;
        private readonly IWindowFactory _windowFactory;
        private readonly SceneManager _sceneManager;

        public OpenSkeletonReshaperToolCommand(SelectionManager selectionManager, SkeletonAnimationLookUpHelper skeletonHelper, PackFileService pfs, IWindowFactory windowFactory, SceneManager sceneManager)
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
                .ToList();

            var allSkeltonNames = meshNodes
                .Select(x => x.Geometry.ParentSkeletonName)
                .Distinct();

            if (allSkeltonNames.Count() != 1)
            {
                var commaList = string.Join(",", allSkeltonNames);
                MessageBox.Show($"Unexpected number of skeletons - {commaList}. This tool only works for one skeleton");
                return;
            }

            var currentSkeletonName = allSkeltonNames.First();
            var currentSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_pfs, currentSkeletonName);

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x => (int)x)
                .ToList();

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var targetSkeleton = rootNode.SkeletonNode;
            var targetSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_pfs, targetSkeleton.Name);

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
