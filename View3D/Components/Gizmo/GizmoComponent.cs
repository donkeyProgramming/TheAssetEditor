using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Linq;
using View3D.Commands;
using View3D.Rendering;
using View3D.Components.Component;
using System.Collections.Generic;
using View3D.Components.Rendering;
using View3D.Components.Input;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.Commands.Vertex;
using View3D.Utility;
using View3D.SceneNodes;

namespace View3D.Components.Gizmo
{
    public class GizmoComponent : BaseComponent, IDisposable
    {
        MouseComponent _mouse;
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;
        CommandExecutor _commandManager;
       
        Gizmo _gizmo;
        bool _isEnabled = false;
        TransformGizmoWrapper _activeTransformation;
        bool _isCtrlPressed = false;

        public GizmoComponent(IComponentManager componentManager) : base(componentManager)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }
       
        public override void Initialize()
        {
            _commandManager = ComponentManager.GetComponent<CommandExecutor>();
            _selectionManager = ComponentManager.GetComponent<SelectionManager>();
            _keyboard = ComponentManager.GetComponent<KeyboardComponent>();
            _mouse = ComponentManager.GetComponent<MouseComponent>();
            var camera = ComponentManager.GetComponent<ArcBallCamera>();
            var resourceLibary = ComponentManager.GetComponent<ResourceLibary>();
            var graphics = ComponentManager.GetComponent<DeviceResolverComponent>();

            _selectionManager.SelectionChanged += OnSelectionChanged;

            _gizmo = new Gizmo(camera, _mouse, graphics.Device, new SpriteBatch(graphics.Device), resourceLibary.DefaultFont);
            _gizmo.ActivePivot = PivotType.ObjectCenter;
            _gizmo.TranslateEvent += GizmoTranslateEvent;
            _gizmo.RotateEvent += GizmoRotateEvent;
            _gizmo.ScaleEvent += GizmoScaleEvent;
            _gizmo.StartEvent += GizmoTransformStart;
            _gizmo.StopEvent += GizmoTransformEnd;
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            _gizmo.Selection.Clear();
            _activeTransformation = TransformGizmoWrapper.CreateFromSelectionState(state);
            if(_activeTransformation != null)
                _gizmo.Selection.Add(_activeTransformation);

            _gizmo.ResetDeltas();
        }

        private void GizmoTransformStart()
        {
            _mouse.MouseOwner = this;
            _activeTransformation.Start(_commandManager);
        }

        private void GizmoTransformEnd()
        {
            _activeTransformation.Stop(_commandManager);
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
        }


        private void GizmoTranslateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoTranslateEvent((Vector3)e.Value, e.Pivot);
        }

        private void GizmoRotateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoRotateEvent((Matrix)e.Value, e.Pivot); 
        }

        private void GizmoScaleEvent(ITransformable transformable, TransformationEventArgs e)
        {
            var value = (Vector3)e.Value;
            if (_isCtrlPressed)
            {
                if(value.X != 0)
                    value = new Vector3(value.X);
                else if (value.Y != 0)
                    value = new Vector3(value.Y);
                else if (value.Z != 0)
                    value = new Vector3(value.Z);
            }

            _activeTransformation.GizmoScaleEvent(value, e.Pivot);
        }

        public override void Update(GameTime gameTime)
        {
            if ( !(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || 
                   _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex ||
                   _selectionManager.GetState().Mode == GeometrySelectionMode.Bone))
                return;

            if (!_isEnabled)
                return;

            _isCtrlPressed = _keyboard.IsKeyDown(Keys.LeftControl);
            if (_gizmo.ActiveMode == GizmoMode.NonUniformScale && _isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.UniformScale;
            else if (_gizmo.ActiveMode == GizmoMode.UniformScale && !_isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            //// Toggle space mode:
            //if (_keyboard.IsKeyReleased(Keys.Home))
            //    _gizmo.ToggleActiveSpace();

            var isCameraMoving = _keyboard.IsKeyDown(Keys.LeftAlt);
            _gizmo.Update(gameTime, !isCameraMoving);
        }

        public void SetGizmoMode(GizmoMode mode)
        {
            _gizmo.ActiveMode = mode;
            _isEnabled = true;
        }

        public void SetGizmoPivot(PivotType type)
        {
            _gizmo.ActivePivot = type;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || 
                  _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex ||
                  _selectionManager.GetState().Mode == GeometrySelectionMode.Bone))
                return;
            if (!_isEnabled)
                return;

            _gizmo.Draw();
        }

        public void ResetScale()
        {
            _gizmo.ScaleModifier = 1;
        }

        public void ModifyGizmoScale(float v)
        {
            _gizmo.ScaleModifier += v;
        }

        public void Dispose()
        {
            _gizmo.Dispose();
        }
    }
}
