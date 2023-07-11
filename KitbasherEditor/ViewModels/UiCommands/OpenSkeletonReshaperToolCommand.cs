using CommonControls.Events.UiCommands;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MeshFitter;
using MonoGame.Framework.WpfInterop;
using View3D.Commands;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenSkeletonReshaperToolCommand : IExecutableUiCommand
    {
        IComponentManager _componentManager;
        SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        PackFileService _packFileService;

        public OpenSkeletonReshaperToolCommand(IComponentManager componentManager, SelectionManager selectionManager, CommandFactory commandFactory, SkeletonAnimationLookUpHelper skeletonHelper, PackFileService packFileService)
        {
            _componentManager = componentManager;
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
