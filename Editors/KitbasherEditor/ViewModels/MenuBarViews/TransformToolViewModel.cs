using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.MathViews;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class TransformToolViewModel : NotifyPropertyChangedImpl
    {
        public enum TransformMode
        {
            None,
            Rotate,
            Scale,
            Translate
        }

        TransformMode _activeMode = TransformMode.None;

        private readonly SelectionManager _selectionManager;
        private readonly CommandExecutor _commandExecutor;
        private readonly CommandFactory _commandFactory;

        public System.Windows.Input.ICommand ApplyCommand { get; set; }

        bool _buttonEnabled = false;
        public bool ButtonEnabled { get { return _buttonEnabled; } set { SetAndNotify(ref _buttonEnabled, value); } }


        bool _isVisible = false;
        public bool IsVisible { get { return _isVisible; } set { SetAndNotify(ref _isVisible, value); } }

        string _text;
        public string Text { get { return _text; } set { SetAndNotify(ref _text, value); } }

        Vector3ViewModel _vector3 = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel Vector3 { get { return _vector3; } set { SetAndNotify(ref _vector3, value); } }

        public NotifyAttr<DoubleViewModel> VertexMovementFalloff { get; set; }

        public NotifyAttr<bool> ShowVertexFalloff { get; set; } = new NotifyAttr<bool>(false);

        public TransformToolViewModel(SelectionManager selectionManager, CommandExecutor commandExecutor, CommandFactory commandFactory, IEventHub eventHub)
        {
            ApplyCommand = new RelayCommand(ApplyTransform);

            VertexMovementFalloff = new NotifyAttr<DoubleViewModel>(new DoubleViewModel());
            VertexMovementFalloff.Value.PropertyChanged += VertexMovementFalloffChanged;
            _selectionManager = selectionManager;
            _commandExecutor = commandExecutor;
            _commandFactory = commandFactory;
            eventHub.Register<SelectionChangedEvent>(this, Handle);
        }

        public void VertexMovementFalloffChanged(object sender, PropertyChangedEventArgs e)
        {
            _selectionManager.UpdateVertexSelectionFallof((float)VertexMovementFalloff.Value.Value);
        }

        private void SelectionChanged(ISelectionState state)
        {
            ShowVertexFalloff.Value = false;
            if (state.Mode == GeometrySelectionMode.Face)
            {
                ButtonEnabled = false;
            }
            else
            {
                if (state is ObjectSelectionState objectSelectionState)
                    ButtonEnabled = objectSelectionState.SelectionCount() != 0;
                else if (state is VertexSelectionState vertexSelectionState)
                {
                    ButtonEnabled = vertexSelectionState.SelectionCount() != 0;
                    ShowVertexFalloff.Value = true;
                }
            }
        }

        public void SetMode(TransformMode mode)
        {
            _activeMode = mode;
            IsVisible = _activeMode != TransformMode.None;

            if (_activeMode == TransformMode.Rotate)
                Text = "Rotate:";
            else if (_activeMode == TransformMode.Scale)
                Text = "Scale:";
            else if (_activeMode == TransformMode.Translate)
                Text = "Translate:";

            SetDefaultValue();
        }

        void ApplyTransform()
        {
            var transform = TransformGizmoWrapper.CreateFromSelectionState(_selectionManager.GetState(), _commandFactory);
            if (transform == null || _activeMode == TransformMode.None)
                return;

            if (_activeMode == TransformMode.Rotate)
            {
                transform.Start(_commandExecutor);
                transform.GizmoRotateEvent(
                    Matrix.CreateRotationX(MathHelper.ToRadians((float)_vector3.X.Value)) *
                    Matrix.CreateRotationY(MathHelper.ToRadians((float)_vector3.Y.Value)) *
                    Matrix.CreateRotationZ(MathHelper.ToRadians((float)_vector3.Z.Value)), PivotType.ObjectCenter);
            }
            else if (_activeMode == TransformMode.Translate)
            {
                transform.Start(_commandExecutor);
                transform.GizmoTranslateEvent(new Vector3((float)_vector3.X.Value, (float)_vector3.Y.Value, (float)_vector3.Z.Value), PivotType.ObjectCenter);
            }
            else if (_activeMode == TransformMode.Scale)
            {
                transform.Start(_commandExecutor);
                transform.GizmoScaleEvent(new Vector3((float)_vector3.X.Value - 1, (float)_vector3.Y.Value - 1, (float)_vector3.Z.Value - 1), PivotType.ObjectCenter);    // -1 due to weirdness inside the function
            }

            transform.Stop(_commandExecutor);
            SetDefaultValue();
        }

        void SetDefaultValue()
        {
            if (_activeMode == TransformMode.Scale)
                _vector3.Set(1);
            else
                _vector3.Set(0);
        }

        void Handle(SelectionChangedEvent notification)
        {
            SelectionChanged(notification.NewState);
        }
    }
}
