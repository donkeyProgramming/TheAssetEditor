using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.PropCreator.ViewModels
{
    public abstract class BaseAnimationViewModel<TEditor> : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public abstract NotifyAttr<string> DisplayName { get; set; }
        public PackFile MainFile { get; set; }

        public NotifyAttr<GameWorld> GameWorld { get; private set; } = new NotifyAttr<GameWorld>();
        public ObservableCollection<SceneObjectViewModel> SceneObjects { get; set; } = new ObservableCollection<SceneObjectViewModel>();


        public AnimationPlayerViewModel Player { get; set; }


        TEditor _editor;
        public TEditor Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        private readonly FocusSelectableObjectService _focusSelectableObjectService;

        public ICommand ResetCameraCommand { get; set; } 
        public ICommand FocusCamerasCommand { get; set; }


        public BaseAnimationViewModel(
            IComponentInserter componentInserter,
            AnimationPlayerViewModel animationPlayerViewModel,
            GameWorld gameWorld,
            FocusSelectableObjectService focusSelectableObjectService)
        {
            GameWorld.Value = gameWorld;
            Player = animationPlayerViewModel;

            _focusSelectableObjectService = focusSelectableObjectService;

            ResetCameraCommand = new RelayCommand(ResetCamera);
            FocusCamerasCommand = new RelayCommand(FocusCamera);

            componentInserter.Execute();
        }

        void ResetCamera() => _focusSelectableObjectService.ResetCamera();
        void FocusCamera() => _focusSelectableObjectService.FocusSelection();

        public void Close()
        {
            GameWorld = null;
        }

        public bool HasUnsavedChanges { get; set; }

        public bool Save() => true;
    }
}
