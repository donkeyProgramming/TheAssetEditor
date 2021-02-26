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
    public class GizmoComponent : BaseComponent
    {
        MouseComponent _mouse;
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;
        CommandExecutor _commandManager;
       
        Gizmo _gizmo;
        bool _isEnabled = false;
        TransformGizmoWrapper _activeTransformation;

        public GizmoComponent(WpfGame game) : base(game)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }
       
        public override void Initialize()
        {
            _commandManager = GetComponent<CommandExecutor>();
            _selectionManager = GetComponent<SelectionManager>();
            _keyboard = GetComponent<KeyboardComponent>();
            _mouse = GetComponent<MouseComponent>();
            var camera = GetComponent<ArcBallCamera>();
            var resourceLibary = GetComponent<ResourceLibary>();

            _selectionManager.SelectionChanged += OnSelectionChanged;

            var font = resourceLibary.Content.Load<SpriteFont>("Fonts\\DefaultFont");
            _gizmo = new Gizmo(camera, _mouse, GraphicsDevice, new SpriteBatch(GraphicsDevice), font);
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
            _activeTransformation = null;
            if (state is ObjectSelectionState objectSelectionState)
            {
                var transformables = objectSelectionState.CurrentSelection().Where(x => x is ITransformable).Select(x=>x.Geometry);
                if (transformables.Any())
                {
                    _activeTransformation = new TransformGizmoWrapper(transformables.ToList());
                    _gizmo.Selection.Add(_activeTransformation);
                }
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                if (vertexSelectionState.SelectedVertices.Count == 0)
                    return;
            
                _activeTransformation = new TransformGizmoWrapper( vertexSelectionState.RenderObject.Geometry, vertexSelectionState.SelectedVertices);
                _gizmo.Selection.Add(_activeTransformation);
            }

            _gizmo.ResetDeltas();
        }

        private void GizmoTransformStart()
        {
            _mouse.MouseOwner = this;
            _activeTransformation.Start(_gizmo.ActiveMode);
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
            _activeTransformation.GizmoTranslateEvent(e);
        }

        private void GizmoRotateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoRotateEvent(e); 
        }

        private void GizmoScaleEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoScaleEvent(e);
        }

        public override void Update(GameTime gameTime)
        {
            if ( !(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;

            if (!_isEnabled)
                return;

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
            if (!(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;
            if (!_isEnabled)
                return;

            _gizmo.Draw();
        }
    }
}
