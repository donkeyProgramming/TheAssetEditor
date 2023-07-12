using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using MonoGame.Framework.WpfInterop;
using View3D.Commands;
using View3D.Components.Component.Selection;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenSkeletonReshaperToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the skeleton modeling tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = null;

        IComponentManager _componentManager;
        SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        PackFileService _packFileService;

        public OpenSkeletonReshaperToolCommand(ComponentManagerResolver componentManagerResolver, SelectionManager selectionManager, CommandFactory commandFactory, SkeletonAnimationLookUpHelper skeletonHelper, PackFileService packFileService)
        {
            _componentManager = componentManagerResolver.ComponentManager;
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
            _skeletonHelper = skeletonHelper;
            _packFileService = packFileService;
        }

        public void Execute()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            MeshFitterViewModel.ShowView(state.CurrentSelection(), _componentManager, _skeletonHelper, _packFileService, _commandFactory);
        }
    }
}
