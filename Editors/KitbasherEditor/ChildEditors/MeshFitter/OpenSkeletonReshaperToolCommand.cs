using System.Windows;
using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenSkeletonReshaperToolCommand : IScopedKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the skeleton modelling tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly SelectionManager _selectionManager;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IPackFileService _pfs;
        private readonly IAbstractFormFactory<MeshFitterWindow> _formFactory;
        private readonly SceneManager _sceneManager;

        public OpenSkeletonReshaperToolCommand(SelectionManager selectionManager, SkeletonAnimationLookUpHelper skeletonHelper, IPackFileService pfs, IAbstractFormFactory<MeshFitterWindow> formFactory, SceneManager sceneManager)
        {
            _selectionManager = selectionManager;
            _skeletonHelper = skeletonHelper;
            _pfs = pfs;
            _formFactory = formFactory;

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

            var window = _formFactory.Create();
            window.ViewModel.Initialize(config, meshNodes, targetSkeleton.Skeleton, currentSkeletonFile);
            window.Show();
        }
    }
}
