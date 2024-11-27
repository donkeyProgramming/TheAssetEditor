using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.BmiEditor;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.Views.EditorViews;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenBmiToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the Bmi tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.OneObjectSelected;
        public Hotkey HotKey { get; } = null;

        private readonly IPackFileService _packFileService;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly SelectionManager _selectionManager;
        private readonly IWindowFactory _windowFactory;

        public OpenBmiToolCommand(IPackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper, SelectionManager selectionManager, IWindowFactory windowFactory)
        {
            _packFileService = packFileService;
            _skeletonHelper = skeletonHelper;
            _selectionManager = selectionManager;
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            var meshNode = state.GetSingleSelectedObject() as Rmv2MeshNode;

            if (meshNode != null)
            {
                var skeletonName = meshNode.Geometry.SkeletonName;
                var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(skeletonName);
                var skeleton = new GameSkeleton(newSkeletonFile, null);

                var window = _windowFactory.Create<BmiViewModel, BmiView>("BMI tool", 1200, 1100);
                window.TypedContext.Initialize(skeleton, meshNode);
                window.ShowWindow();
            }
        }
    }
}
