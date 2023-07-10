using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using View3D.Components;
using View3D.Scene;
using View3D.Services;

namespace AnimationEditor.PropCreator.ViewModels
{
    public abstract class BaseAnimationViewModel<TEditor> : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Creator");
        public PackFile MainFile { get; set; }


        MainScene _scene;
        public MainScene Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public NotifyAttr<ReferenceModelSelectionViewModel> MainModelView { get; set; } = new NotifyAttr<ReferenceModelSelectionViewModel>();
        public NotifyAttr<ReferenceModelSelectionViewModel> ReferenceModelView { get; set; } = new NotifyAttr<ReferenceModelSelectionViewModel>();
        public AnimationPlayerViewModel Player { get; set; }


        public AnimationToolInput MainInput { get; set; } = new AnimationToolInput();
        public AnimationToolInput RefInput { get; set; }


        TEditor _editor;
        public TEditor Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        private readonly FocusSelectableObjectService _focusSelectableObjectService;

        public ICommand ResetCameraCommand { get; set; }
        public ICommand FocusCamerasCommand { get; set; }


        public BaseAnimationViewModel(IComponentInserter componentInserter,
            AnimationPlayerViewModel animationPlayerViewModel,
            MainScene sceneContainer,
            FocusSelectableObjectService focusSelectableObjectService)
        {
            Scene = sceneContainer;
            _focusSelectableObjectService = focusSelectableObjectService;
            Player = animationPlayerViewModel;

            ResetCameraCommand = new RelayCommand(ResetCamera);
            FocusCamerasCommand = new RelayCommand(FocusCamera);

            componentInserter.Execute();
        }

        void ResetCamera() => _focusSelectableObjectService.ResetCamera();
        void FocusCamera() => _focusSelectableObjectService.FocusSelection();

        public void Close()
        {
            Scene = null;
        }

        public bool HasUnsavedChanges { get; set; }

        public bool Save() => true;
    }
}
