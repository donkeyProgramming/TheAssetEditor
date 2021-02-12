using Common;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;
using View3D.Components.Gizmo;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GizmoModeMenuBarViewModel : NotifyPropertyChangedImpl
    {
        public ICommand MoveCommand { get; set; }
        public ICommand RotateCommand { get; set; }
        public ICommand ScaleCommand { get; set; }

        bool _moveActive = true;
        public bool MoveActive { get { return _moveActive; } set { SetAndNotify(ref _moveActive, value); } }


        bool _rotateActive;
        public bool RotateActive { get { return _rotateActive; } set { SetAndNotify(ref _rotateActive, value); } }

        bool _scaleActive;
        public bool ScaleActive { get { return _scaleActive; } set { SetAndNotify(ref _scaleActive, value); } }


        ToolbarCommandFactory _commandFactory;
        GizmoComponent _gizmoComponent;

        public GizmoModeMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            _commandFactory = commandFactory;

            MoveCommand = _commandFactory.Register(new RelayCommand(Move), Key.W, ModifierKeys.None);
            RotateCommand = _commandFactory.Register(new RelayCommand(Rotate), Key.E, ModifierKeys.None);
            ScaleCommand = _commandFactory.Register(new RelayCommand(Scale), Key.R, ModifierKeys.None);

            _gizmoComponent = componentManager.GetComponent<GizmoComponent>();
        }

        void Move()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);
            MoveActive = true;
            RotateActive = false;
            ScaleActive = false;
        }

        void Rotate()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
            MoveActive = false;
            RotateActive = true;
            ScaleActive = false;
        }

        void Scale()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
            MoveActive = false;
            RotateActive = false;
            ScaleActive = true;
        }
    }
}
