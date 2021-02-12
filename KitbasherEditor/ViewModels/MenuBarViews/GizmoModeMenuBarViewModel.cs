using Common;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;
using View3D.Components.Gizmo;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GizmoModeMenuBarViewModel : NotifyPropertyChangedImpl
    {
        public ICommand CursorCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand RotateCommand { get; set; }
        public ICommand ScaleCommand { get; set; }

        bool _cursorActive = true;
        public bool CursorActive { get { return _cursorActive; } set { SetAndNotify(ref _cursorActive, value); } }

        bool _moveActive;
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

            MoveCommand = _commandFactory.Register(new RelayCommand(Cursor), Key.Q, ModifierKeys.None);
            MoveCommand = _commandFactory.Register(new RelayCommand(Move), Key.W, ModifierKeys.None);
            RotateCommand = _commandFactory.Register(new RelayCommand(Rotate), Key.E, ModifierKeys.None);
            ScaleCommand = _commandFactory.Register(new RelayCommand(Scale), Key.R, ModifierKeys.None);

            _gizmoComponent = componentManager.GetComponent<GizmoComponent>();
        }

        void Cursor()
        {
            _gizmoComponent.Disable();
            CursorActive = true;
            MoveActive = false;
            RotateActive = false;
            ScaleActive = false;
        }

        void Move()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);
            CursorActive = false;
            MoveActive = true;
            RotateActive = false;
            ScaleActive = false;
        }

        void Rotate()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
            CursorActive = false;
            MoveActive = false;
            RotateActive = true;
            ScaleActive = false;
        }

        void Scale()
        {
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
            CursorActive = false;
            MoveActive = false;
            RotateActive = false;
            ScaleActive = true;
        }
    }
}
