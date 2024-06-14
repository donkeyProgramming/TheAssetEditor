using System.Collections.ObjectModel;
using System.Windows.Input;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components;
using GameWorld.Core.Services;
using GameWorld.WpfWindow;
using GameWorld.WpfWindow.Events;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace AnimationEditor.PropCreator.ViewModels
{
    public interface IHostedEditor<T>
    {
        void Initialize(EditorHost<T> owner);
        string EditorName { get; }
    }

    public class EditorHost<TEditor> : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public IToolFactory ToolsFactory { get; set; }
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Name missing");
        public PackFile MainFile { get; set; }

        public NotifyAttr<WpfGame> GameWorld { get; private set; } = new NotifyAttr<WpfGame>();
        public ObservableCollection<SceneObjectViewModel> SceneObjects { get; set; } = new ObservableCollection<SceneObjectViewModel>();
        public AnimationPlayerViewModel Player { get; set; }

        public TEditor Editor { get; set; }

        private readonly FocusSelectableObjectService _focusSelectableObjectService;

        public ICommand ResetCameraCommand { get; set; } 
        public ICommand FocusCamerasCommand { get; set; }

        public EditorHost(IToolFactory toolFactory,
            IComponentInserter componentInserter,
            AnimationPlayerViewModel animationPlayerViewModel,
            WpfGame gameWorld,
            FocusSelectableObjectService focusSelectableObjectService,
            TEditor editor,
            EventHub eventHub)
        {
            ToolsFactory = toolFactory;
            Editor = editor;
            GameWorld.Value = gameWorld;
            Player = animationPlayerViewModel;

            _focusSelectableObjectService = focusSelectableObjectService;

            ResetCameraCommand = new RelayCommand(ResetCameraAction);
            FocusCamerasCommand = new RelayCommand(FocusCameraAction);

            componentInserter.Execute();

            eventHub.Register<SceneInitializedEvent>(Initialize);

            var typed = Editor as IHostedEditor<TEditor>;
            DisplayName.Value = typed.EditorName;
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var typed = Editor as IHostedEditor<TEditor>;
            typed.Initialize(this);
        }

        void ResetCameraAction() => _focusSelectableObjectService.ResetCamera();
        void FocusCameraAction() => _focusSelectableObjectService.FocusSelection();

        public void Close()
        {
            GameWorld = null;
        }
    }
}
