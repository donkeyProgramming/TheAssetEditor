using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using CommonControls.Services;
using KitbasherEditor.ViewModels.BmiEditor;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.Views.EditorViews;
using System.Windows;
using View3D.Animation;
using View3D.Commands;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenBmiToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the Bmi tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.OneObjectSelected;
        public Hotkey HotKey { get; } = null;

        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        public OpenBmiToolCommand(PackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper, SelectionManager selectionManager, CommandFactory commandFactory)
        {
            _packFileService = packFileService;
            _skeletonHelper = skeletonHelper;
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
        }

        public void Execute()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            var meshNode = state.GetSingleSelectedObject() as Rmv2MeshNode;

            if (meshNode != null)
            {
                var skeletonName = meshNode.Geometry.ParentSkeletonName;

                var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, skeletonName);
                GameSkeleton skeleton = new GameSkeleton(newSkeletonFile, null);

                var window = new ControllerHostWindow(true, ResizeMode.CanResize)
                {
                    DataContext = new BmiViewModel(skeleton, meshNode, _commandFactory),
                    Title = "Bmi Tool",
                    Content = new BmiView(),
                };

                window.Show();
            }
        }
    }
}
