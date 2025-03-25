using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Core.PackFiles;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenBmiToolCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the Bmi tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.OneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly IPackFileService _packFileService;
        private readonly ISkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly SelectionManager _selectionManager;

        public OpenBmiToolCommand(IPackFileService packFileService, ISkeletonAnimationLookUpHelper skeletonHelper, SelectionManager selectionManager)
        {
            _packFileService = packFileService;
            _skeletonHelper = skeletonHelper;
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            //var state = _selectionManager.GetState<ObjectSelectionState>();
            //var meshNode = state.GetSingleSelectedObject() as Rmv2MeshNode;
            //
            //if (meshNode != null)
            //{
            //    var skeletonName = meshNode.Geometry.SkeletonName;
            //    var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(skeletonName);
            //    var skeleton = new GameSkeleton(newSkeletonFile, null);
            //
            //    var window = _windowFactory.Create<BmiViewModel, BmiView>("BMI tool", 1200, 1100);
            //    window.TypedContext.Initialize(skeleton, meshNode);
            //    window.ShowWindow();
            //}
        }
    }
}
