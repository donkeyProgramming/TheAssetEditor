using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Components.Component.Selection
{

    public class SelectionComponent : BaseComponent
    {
        ILogger _logger = Logging.Create<SelectionManager>();

        SpriteBatch _spriteBatch;
        Texture2D _textTexture;

        KeyboardComponent _keyboardComponent;
        MouseComponent _mouseComponent;
        ArcBallCamera _camera;
        SelectionManager _selectionManager;
        SceneManager _sceneManger;
        CommandManager _commandManager;

        bool _isMouseDown = false;
        Vector2 _startDrag;
        Vector2 _currentMousePos;

        public SelectionComponent(WpfGame game) : base(game) { }

        public override void Initialize()
        {
            _mouseComponent = GetComponent<MouseComponent>();
            _keyboardComponent = GetComponent<KeyboardComponent>();
            _camera = GetComponent<ArcBallCamera>();
            _sceneManger = GetComponent<SceneManager>();
            _selectionManager = GetComponent<SelectionManager>();
            _commandManager = GetComponent<CommandManager>();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _textTexture = new Texture2D(GraphicsDevice, 1, 1);
            _textTexture.SetData(new Color[1 * 1] { Color.White });

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_mouseComponent.IsMouseOwner(this))
                return;

            ChangeSelectionState();

            _currentMousePos = _mouseComponent.Position();

            if (_mouseComponent.IsMouseButtonPressed(MouseButton.Left))
            {
                _startDrag = _mouseComponent.Position();
                _isMouseDown = true;

                if (_mouseComponent.MouseOwner != this)
                    _mouseComponent.MouseOwner = this;
            }

            if (_mouseComponent.IsMouseButtonReleased(MouseButton.Left) && _isMouseDown)
            {
                var selectionRectangle = CreateSelectionRectangle(_startDrag, _currentMousePos);

                var rectArea = RectArea(selectionRectangle);
                if (rectArea > 8)
                    SelectFromRectangle(selectionRectangle, _keyboardComponent.IsKeyDown(Keys.LeftShift));
                else
                    SelectFromPoint(_currentMousePos, _keyboardComponent.IsKeyDown(Keys.LeftShift));

                _isMouseDown = false;
            }


            if (!_isMouseDown)
            {
                if (_mouseComponent.MouseOwner == this)
                {
                    _mouseComponent.MouseOwner = null;
                    _mouseComponent.ClearStates();
                    return;
                }
            }
        }

        void SelectFromRectangle(Rectangle screenRect, bool isSelectionModification)
        {
            var unprojectedSelectionRect = _camera.UnprojectRectangle(screenRect);

        
            var currentState = _selectionManager.GetState();
            if (currentState.Mode == GeometrySelectionMode.Face)
            {
            }


            var selectedObjects = PickingUtil.SelectObjects(unprojectedSelectionRect, _sceneManger);
            if (selectedObjects.Count() == 0 && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                    selectionCommand.ClearSelection = true;
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
            else if (selectedObjects != null)
            {
                var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                selectionCommand.IsModification = isSelectionModification;
                foreach(var item in selectedObjects)
                    selectionCommand.Items.Add(item);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }

        void SelectFromPoint(Vector2 mousePosition, bool isSelectionModification)
        {
            var ray = _camera.CreateCameraRay(mousePosition);
            var currentState = _selectionManager.GetState();
            if (currentState.Mode == GeometrySelectionMode.Face)
            {
                if (currentState.Mode == GeometrySelectionMode.Face)
                {
                    var faceState = currentState as FaceSelectionState;

                    if (faceState.RenderObject.Geometry.IntersectFace(ray, faceState.RenderObject.ModelMatrix, out var selectedFace) != null)
                    {
                        _logger.Here().Information($"Selected face {selectedFace} in {faceState.RenderObject.Name}");

                        FaceSelectionCommand faceSelectionCommand = new FaceSelectionCommand(_selectionManager)
                        {
                            IsModification = isSelectionModification,
                            SelectedFaces = new List<int>() { selectedFace.Value }
                        };
                        _commandManager.ExecuteCommand(faceSelectionCommand);
                        return;
                    }
                }
            }

            // Pick object
            var selectedObject = PickingUtil.SelectObject(ray, _sceneManger);
            if (selectedObject == null && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                    selectionCommand.ClearSelection = true;
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
            else if(selectedObject != null)
            {
                var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                selectionCommand.IsModification = isSelectionModification;
                selectionCommand.Items.Add(selectedObject);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }

        bool ChangeSelectionState()
        {
            var selectionState = _selectionManager.GetState();

            if (_keyboardComponent.IsKeyReleased(Keys.F1) && _selectionManager.GetState().Mode != GeometrySelectionMode.Object)
            {
                _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(_selectionManager, GeometrySelectionMode.Object));
                return true;
            }

            else if (_keyboardComponent.IsKeyReleased(Keys.F2) && _selectionManager.GetState().Mode != GeometrySelectionMode.Face)
            {
                var objectSelectonState = selectionState as ObjectSelectionState;
                if (objectSelectonState.CurrentSelection().Count == 1)
                {
                    _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(objectSelectonState.CurrentSelection().First(), _selectionManager, GeometrySelectionMode.Face));
                    return true;
                }
            }

            return false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (_isMouseDown)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                var dest = CreateSelectionRectangle(_startDrag, _currentMousePos);

                _spriteBatch.Draw(_textTexture, dest, Color.White * 0.5f);

                var lineWidth = 2;
                var top = new Rectangle(dest.X, dest.Y, dest.Width, lineWidth);
                var bottom = new Rectangle(dest.X, dest.Y + dest.Height, dest.Width + 2, lineWidth);
                var left = new Rectangle(dest.X, dest.Y, lineWidth, dest.Height);
                var right = new Rectangle(dest.X + dest.Width, dest.Y, lineWidth, dest.Height);
                _spriteBatch.Draw(_textTexture, top, Color.Red * 0.75f);
                _spriteBatch.Draw(_textTexture, bottom, Color.Red * 0.75f);
                _spriteBatch.Draw(_textTexture, left, Color.Red * 0.75f);
                _spriteBatch.Draw(_textTexture, right, Color.Red * 0.75f);
                _spriteBatch.End();
            }
        }

        Rectangle CreateSelectionRectangle(Vector2 start, Vector2 stop)
        {
            var width = Math.Abs((int)(_currentMousePos.X - _startDrag.X));
            var height = Math.Abs((int)(_currentMousePos.Y - _startDrag.Y));

            var x = (int)Math.Min(start.X, stop.X);
            var y = (int)Math.Min(start.Y, stop.Y);

            return new Rectangle(x, y, width, height);
        }

        int RectArea(Rectangle rect)
        {
            return rect.Width * rect.Height;
        }
    }
}

