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

namespace View3D.Components.Gizmo
{
    public delegate void GizmoUpdated();

    public class GizmoComponent : BaseComponent
    {
        MouseComponent _mouse;
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;
        CommandManager _commandManager;
        Gizmo _gizmo;
        SpriteBatch _spriteBatch;
        TransformCommand _activeCommand;

        public GizmoComponent(WpfGame game) : base(game)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }

        public override void Initialize()
        {
            _commandManager = GetComponent<CommandManager>();
            _selectionManager = GetComponent<SelectionManager>();
            _keyboard = GetComponent<KeyboardComponent>();
            _mouse = GetComponent<MouseComponent>();
            var camera = GetComponent<ArcBallCamera>();
            var resourceLibary = GetComponent<ResourceLibary>();

            _selectionManager.SelectionChanged += OnSelectionChanged;

            var font = resourceLibary.XnaContentManager.Load<SpriteFont>("Fonts\\DefaultFont");
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gizmo = new Gizmo(camera, _mouse, GraphicsDevice, _spriteBatch, font);

            _gizmo.TranslateEvent += GizmoTranslateEvent;
            _gizmo.RotateEvent += GizmoRotateEvent;
            _gizmo.ScaleEvent += GizmoScaleEvent;
            _gizmo.StartEvent += GizmoTransformStart;
            _gizmo.StopEvent += GizmoTransformEnd;
        }

        private void GizmoTransformStart()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Object)
                return;

            _activeCommand = new TransformCommand((_selectionManager.GetState() as ObjectSelectionState).CurrentSelection());
        }

        private void GizmoTransformEnd()
        {
            if (_activeCommand != null)
            {
                _commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
                _mouse.ClearStates();
            }
        }


        private void OnSelectionChanged(IEnumerable<RenderItem> items)
        {
            _gizmo.Selection.Clear();
            foreach (var item in items)
                _gizmo.Selection.Add(item);

            _gizmo.ResetDeltas();
        }


        private void GizmoTranslateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Position += (Vector3)e.Value;
        }  

        private void GizmoRotateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Orientation = Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(transformable.Orientation) * (Matrix)e.Value);
        }

        private void GizmoScaleEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Scale += (Vector3)e.Value;
        }

        public override void Update(GameTime gameTime)
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Object)
                return;
            _gizmo.UpdateCameraProperties();

            // Toggle transform mode:
            if (_keyboard.IsKeyReleased(Keys.R))
                _gizmo.ActiveMode = GizmoMode.Rotate;

            if (_keyboard.IsKeyReleased(Keys.T))
                _gizmo.ActiveMode = GizmoMode.Translate;

            if (_keyboard.IsKeyReleased(Keys.Y))
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            // Toggle space mode:
            if (_keyboard.IsKeyReleased(Keys.Home))
                _gizmo.ToggleActiveSpace();

            // Workaround for camera roation causing movment
            if (!_keyboard.IsKeyDown(Keys.LeftAlt))
                _gizmo.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Object)
                return;
            _gizmo.Draw(false);
        }
    }
}
