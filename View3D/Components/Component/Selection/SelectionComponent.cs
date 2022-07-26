using CommonControls.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Face;
using View3D.Commands.Object;
using View3D.Commands.Vertex;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Components.Component.Selection
{

    public class SelectionComponent : BaseComponent, IDisposable
    {
        ILogger _logger = Logging.Create<SelectionManager>();

        SpriteBatch _spriteBatch;
        Texture2D _textTexture;

        KeyboardComponent _keyboardComponent;
        MouseComponent _mouseComponent;
        ArcBallCamera _camera;
        SelectionManager _selectionManager;
        SceneManager _sceneManger;
        CommandExecutor _commandManager;

        bool _isMouseDown = false;
        Vector2 _startDrag;
        Vector2 _currentMousePos;

        public SelectionComponent(IComponentManager componentManager) : base(componentManager) { }

        public override void Initialize()
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.SelectionComponent;
            DrawOrder = (int)ComponentDrawOrderEnum.SelectionComponent;

            _mouseComponent = ComponentManager.GetComponent<MouseComponent>();
            _keyboardComponent = ComponentManager.GetComponent<KeyboardComponent>();
            _camera = ComponentManager.GetComponent<ArcBallCamera>();
            _sceneManger = ComponentManager.GetComponent<SceneManager>();
            _selectionManager = ComponentManager.GetComponent<SelectionManager>();
            _commandManager = ComponentManager.GetComponent<CommandExecutor>();
            var graphicsResolver = ComponentManager.GetComponent<DeviceResolverComponent>();

            _spriteBatch = new SpriteBatch(graphicsResolver.Device);
            _textTexture = new Texture2D(graphicsResolver.Device, 1, 1);
            _textTexture.SetData(new Color[1 * 1] { Color.White });

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_mouseComponent.IsMouseOwner(this))
                return;

            ChangeSelectionMode();

            _currentMousePos = _mouseComponent.Position();

            if (_mouseComponent.IsMouseButtonPressed(MouseButton.Left))
            {
                _startDrag = _mouseComponent.Position();
                _isMouseDown = true;

                if (_mouseComponent.MouseOwner != this)
                    _mouseComponent.MouseOwner = this;
            }

            if (_mouseComponent.IsMouseButtonReleased(MouseButton.Left))
            {
                if (_isMouseDown)
                {
                    var selectionRectangle = CreateSelectionRectangle(_startDrag, _currentMousePos);
                    var isSelectionRect = IsSelectionRectangle(selectionRectangle);
                    if (isSelectionRect)
                        SelectFromRectangle(selectionRectangle, _keyboardComponent.IsKeyDown(Keys.LeftShift), _keyboardComponent.IsKeyDown(Keys.LeftControl));
                    else
                        SelectFromPoint(_currentMousePos, _keyboardComponent.IsKeyDown(Keys.LeftShift), _keyboardComponent.IsKeyDown(Keys.LeftControl));
                }
                else
                    SelectFromPoint(_currentMousePos, _keyboardComponent.IsKeyDown(Keys.LeftShift), _keyboardComponent.IsKeyDown(Keys.LeftControl));

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

        void SelectFromRectangle(Rectangle screenRect, bool isSelectionModification, bool removeSelection)
        {
            var unprojectedSelectionRect = _camera.UnprojectRectangle(screenRect);
  
            var currentState = _selectionManager.GetState();
            if (currentState.Mode == GeometrySelectionMode.Face && currentState is FaceSelectionState faceState)
            {
                if (GeometryIntersection.IntersectFaces(unprojectedSelectionRect, faceState.RenderObject.Geometry, faceState.RenderObject.RenderMatrix, out var faces))
                {
                    var faceSelectionCommand = new FaceSelectionCommand(faces, isSelectionModification, removeSelection);
                    _commandManager.ExecuteCommand(faceSelectionCommand);
                    return;
                }
            }
            else if (currentState.Mode == GeometrySelectionMode.Vertex && currentState is VertexSelectionState vertexState)
            {
                if (GeometryIntersection.IntersectVertices(unprojectedSelectionRect, vertexState.RenderObject.Geometry, vertexState.RenderObject.RenderMatrix, out var vertices))
                {
                    var vertexSelectionCommand = new VertexSelectionCommand(vertices, isSelectionModification, removeSelection);
                    _commandManager.ExecuteCommand(vertexSelectionCommand);
                    return;
                }
            }

            var selectedObjects = _sceneManger.SelectObjects(unprojectedSelectionRect);
            if (selectedObjects.Count() == 0 && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(new List<ISelectable>(), false, false);
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
            else if (selectedObjects != null)
            {
                var selectionCommand = new ObjectSelectionCommand(selectedObjects, isSelectionModification, removeSelection);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }

        void SelectFromPoint(Vector2 mousePosition, bool isSelectionModification, bool removeSelection)
        {
            var ray = _camera.CreateCameraRay(mousePosition);
            var currentState = _selectionManager.GetState();
            if (currentState is FaceSelectionState faceState)
            {  
                if (GeometryIntersection.IntersectFace(ray, faceState.RenderObject.Geometry, faceState.RenderObject.RenderMatrix, out var selectedFace) != null)
                {
                    FaceSelectionCommand faceSelectionCommand = new FaceSelectionCommand(selectedFace.Value, isSelectionModification, removeSelection);
                    _commandManager.ExecuteCommand(faceSelectionCommand);
                    return;
                }
            }

            if (currentState is VertexSelectionState vertexState)
            {
                if (GeometryIntersection.IntersectVertex(ray, vertexState.RenderObject.Geometry, _camera.Position, vertexState.RenderObject.RenderMatrix, out var selecteVert) != null)
                {
                    var selectCommand = new VertexSelectionCommand(new List<int>() { selecteVert }, isSelectionModification, removeSelection);
                    _commandManager.ExecuteCommand(selectCommand);
                    return;
                }
            }

            // Pick object
            var selectedObject = _sceneManger.SelectObject(ray);
            if (selectedObject == null && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(new List<ISelectable>(), false, false);
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
            else if(selectedObject != null)
            {
                var selectionCommand = new ObjectSelectionCommand(selectedObject, isSelectionModification, removeSelection);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }

        public bool SetObjectSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Object)
            {
                _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(selectionState.GetSingleSelectedObject(), GeometrySelectionMode.Object));
                return true;
            }
            return false;
        }

        public bool SetFaceSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Face)
            {
                var selectedObject = selectionState.GetSingleSelectedObject();
                if (selectedObject != null)
                {
                    _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(selectedObject, GeometrySelectionMode.Face));
                    return true;
                }

            }
            return false;
        }

        public bool SetVertexSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if ( _selectionManager.GetState().Mode != GeometrySelectionMode.Vertex)
            {
                var selectedObject = selectionState.GetSingleSelectedObject();
                if (selectedObject != null)
                {
                    _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(selectedObject, GeometrySelectionMode.Vertex));
                    return true;
                }
            }
            return false;
        }

        bool ChangeSelectionMode()
        {
            if (_keyboardComponent.IsKeyReleased(Keys.F1))
            {
                if (SetObjectSelectionMode())
                    return true;
            }

            else if (_keyboardComponent.IsKeyReleased(Keys.F2))
            {
                if (SetFaceSelectionMode())
                    return true;
            }

            else if (_keyboardComponent.IsKeyReleased(Keys.F3))
            {
                if (SetVertexSelectionMode())
                    return true;
            }

            return false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (_isMouseDown)
            {
                var dest = CreateSelectionRectangle(_startDrag, _currentMousePos);
                var lineWidth = 2;
                var top = new Rectangle(dest.X, dest.Y, dest.Width, lineWidth);
                var bottom = new Rectangle(dest.X, dest.Y + dest.Height, dest.Width + 2, lineWidth);
                var left = new Rectangle(dest.X, dest.Y, lineWidth, dest.Height);
                var right = new Rectangle(dest.X + dest.Width, dest.Y, lineWidth, dest.Height);

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _spriteBatch.Draw(_textTexture, dest, Color.White * 0.5f);
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

        bool IsSelectionRectangle(Rectangle rect)
        {
            var area = rect.Width * rect.Height;
            if (area < 10)
                return false;

            return true;
        }

        public void Dispose()
        {
            _spriteBatch.Dispose();
            _textTexture.Dispose();
        }
    }
}

