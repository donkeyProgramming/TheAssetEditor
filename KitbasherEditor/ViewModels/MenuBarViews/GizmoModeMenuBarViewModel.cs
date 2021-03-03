using Common;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System;
using System.Windows.Input;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GizmoModeMenuBarViewModel : NotifyPropertyChangedImpl
    {
        public ICommand CursorCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand RotateCommand { get; set; }
        public ICommand ScaleCommand { get; set; }

        public ICommand ScaleGizmoUpCommand { get; set; }

        public ICommand ScaleGizmoDownCommand { get; set; }

        bool _cursorActive = true;
        public bool CursorActive { get { return _cursorActive; } set { SetAndNotify(ref _cursorActive, value); } }

        bool _moveActive;
        public bool MoveActive { get { return _moveActive; } set { SetAndNotify(ref _moveActive, value); } }

        bool _rotateActive;
        public bool RotateActive { get { return _rotateActive; } set { SetAndNotify(ref _rotateActive, value); } }

        bool _scaleActive;
        public bool ScaleActive { get { return _scaleActive; } set { SetAndNotify(ref _scaleActive, value); } }


        int _selectionModeIndex = 0;
        public int SelectionModeIndex 
        { 
            get { return _selectionModeIndex; } 
            set 
            {
                if (value != _selectionModeIndex)
                {
                    SetAndNotify(ref _selectionModeIndex, value);
                    UpdateSelectionMode(value);
                }
            } 
        }

        int _pivotModeModeIndex = 1;
        public int PivotModeModeIndex { get { return _pivotModeModeIndex; } set { SetAndNotify(ref _pivotModeModeIndex, value); UpdatePivotMode(value); } }

        ToolbarCommandFactory _commandFactory;
        GizmoComponent _gizmoComponent;
        SelectionManager _selectionManager;
        SelectionComponent _selectionComponent;
        TransformToolViewModel _transformToolViewModel;

        public GizmoModeMenuBarViewModel(TransformToolViewModel transformToolViewModel, IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            _transformToolViewModel = transformToolViewModel;
            _commandFactory = commandFactory;

            CursorCommand = _commandFactory.Register(new RelayCommand(Cursor), Key.Q, ModifierKeys.None);
            MoveCommand = _commandFactory.Register(new RelayCommand(Move), Key.W, ModifierKeys.None);
            RotateCommand = _commandFactory.Register(new RelayCommand(Rotate), Key.E, ModifierKeys.None);
            ScaleCommand = _commandFactory.Register(new RelayCommand(Scale), Key.R, ModifierKeys.None);

            ScaleGizmoUpCommand = _commandFactory.Register(new RelayCommand(ScaleGizmoUp), Key.Add, ModifierKeys.None);
            ScaleGizmoDownCommand = _commandFactory.Register(new RelayCommand(ScaleGizmoDown), Key.Subtract, ModifierKeys.None);

            _gizmoComponent = componentManager.GetComponent<GizmoComponent>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;
            _selectionComponent = componentManager.GetComponent<SelectionComponent>();
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            if (state.Mode == GeometrySelectionMode.Object)
                SelectionModeIndex = 0;
            else if (state.Mode == GeometrySelectionMode.Face)
                SelectionModeIndex = 1;
            else if (state.Mode == GeometrySelectionMode.Vertex)
                SelectionModeIndex = 2;
            else
                throw new NotImplementedException("Unkown state");
        }

        void UpdateSelectionMode(int index)
        {
            if (index == 0)
                _selectionComponent.SetObjectSelectionMode();
            else if(index == 1)
                _selectionComponent.SetFaceSelectionMode();
            else if (index == 2)
                _selectionComponent.SetVertexSelectionMode();
            else
                throw new NotImplementedException("Unkown state");
        }

        private void UpdatePivotMode(int value)
        {
            if (value == 0)
                _gizmoComponent.SetGizmoPivot(PivotType.SelectionCenter);
            else if (value == 1)
                _gizmoComponent.SetGizmoPivot(PivotType.ObjectCenter);
            //else if (value == 2)
            //    _gizmoComponent.SetGizmoPivot(PivotType.WorldOrigin);
            else
                throw new NotImplementedException("Unkown state");
        }


        void Cursor()
        {
            ResetGizmoSize();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.None);
            _gizmoComponent.Disable();
            CursorActive = true;
            MoveActive = false;
            RotateActive = false;
            ScaleActive = false;
        }

        void Move()
        {
            ResetGizmoSize();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Translate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);
            CursorActive = false;
            MoveActive = true;
            RotateActive = false;
            ScaleActive = false;
        }

        void Rotate()
        {
            ResetGizmoSize();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Rotate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
            CursorActive = false;
            MoveActive = false;
            RotateActive = true;
            ScaleActive = false;
        }

        void Scale()
        {
            ResetGizmoSize();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Scale);
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
            CursorActive = false;
            MoveActive = false;
            RotateActive = false;
            ScaleActive = true;
        }

        void ResetGizmoSize()
        {
            _gizmoComponent.ResetScale();
        }

        private void ScaleGizmoDown()
        {
            _gizmoComponent.ModifyGizmoScale(-0.5f);
        }

        private void ScaleGizmoUp()
        {
            _gizmoComponent.ModifyGizmoScale(0.5f);
        }

    }
}
