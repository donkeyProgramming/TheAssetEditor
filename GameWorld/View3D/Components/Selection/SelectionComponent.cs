using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Commands.Face;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Commands.Vertex;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using MouseButton = GameWorld.Core.Components.Input.MouseButton;

namespace GameWorld.Core.Components.Selection
{
    public class SelectionComponent : BaseComponent, IDisposable
    {
        //SpriteBatch _spriteBatch;
        Texture2D _textTexture;
        bool _isMouseDown = false;
        Vector2 _startDrag;
        Vector2 _currentMousePos;

        private readonly IKeyboardComponent _keyboardComponent;
        private readonly IMouseComponent _mouseComponent;
        private readonly ArcBallCamera _camera;
        private readonly SelectionManager _selectionManager;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly CommandFactory _commandFactory;
        private readonly SceneManager _sceneManger;
        private readonly RenderEngineComponent _resourceLibrary;

        public SelectionComponent(
            IMouseComponent mouseComponent, IKeyboardComponent keyboardComponent,
            ArcBallCamera camera, SelectionManager selectionManager,
            IDeviceResolver deviceResolverComponent, CommandFactory commandFactory,
            SceneManager sceneManager, RenderEngineComponent resourceLibrary)
        {
            _mouseComponent = mouseComponent;
            _keyboardComponent = keyboardComponent;
            _camera = camera;
            _selectionManager = selectionManager;
            _deviceResolverComponent = deviceResolverComponent;
            _commandFactory = commandFactory;
            _sceneManger = sceneManager;
            _resourceLibrary = resourceLibrary;
        }

        public override void Initialize()
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.SelectionComponent;
            DrawOrder = (int)ComponentDrawOrderEnum.SelectionComponent;

            //_spriteBatch = new SpriteBatch(_deviceResolverComponent.Device);
            _textTexture = new Texture2D(_deviceResolverComponent.Device, 1, 1);
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

        public void SelectFromIndex(int index)
        {
            var selectable = _sceneManger.GetByIndex(index);
            if (selectable == null)
                return;

            //_selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, selectable, true);
            _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure([selectable], false, true)).BuildAndExecute();
        }

        void SelectFromRectangle(Rectangle screenRect, bool isSelectionModification, bool removeSelection)
        {
            var unprojectedSelectionRect = _camera.UnprojectRectangle(screenRect);

            var currentState = _selectionManager.GetState();
            if (currentState.Mode == GeometrySelectionMode.Face && currentState is FaceSelectionState faceState)
            {
                if (IntersectionMath.IntersectFaces(unprojectedSelectionRect, faceState.RenderObject.Geometry, faceState.RenderObject.RenderMatrix, out var faces))
                {
                    _commandFactory.Create<FaceSelectionCommand>().Configure(x => x.Configure(faces, isSelectionModification, removeSelection)).BuildAndExecute();
                    return;
                }
            }
            else if (currentState.Mode == GeometrySelectionMode.Vertex && currentState is VertexSelectionState vertexState)
            {
                if (IntersectionMath.IntersectVertices(unprojectedSelectionRect, vertexState.RenderObject.Geometry, vertexState.RenderObject.RenderMatrix, out var vertices))
                {
                    _commandFactory.Create<VertexSelectionCommand>().Configure(x => x.Configure(vertices, isSelectionModification, removeSelection)).BuildAndExecute();
                    return;
                }
            }
            else if (currentState.Mode == GeometrySelectionMode.Bone && currentState is BoneSelectionState boneState)
            {
                if (boneState.RenderObject == null)
                {
                    MessageBox.Show("no object was selected. select an object first then you can select the bone", "no object", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (boneState.CurrentAnimation == null)
                {
                    MessageBox.Show("no animation was played. select a frame by scrubbing the animation using the control below", "no animation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var vertexObject = boneState.RenderObject as Rmv2MeshNode;
                if (IntersectionMath.IntersectBones(unprojectedSelectionRect, vertexObject, boneState.Skeleton, vertexObject.RenderMatrix, out var bones))
                {
                    foreach (var bone in bones)
                    {
                        Console.WriteLine($"bone id: {bone}");
                    }
                    _commandFactory.Create<BoneSelectionCommand>().Configure(x => x.Configure(bones, isSelectionModification, removeSelection)).BuildAndExecute();
                    return;
                }
            }

            var selectedObjects = _sceneManger.SelectObjects(unprojectedSelectionRect);
            if (selectedObjects.Count() == 0 && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                    _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(new List<ISelectable>(), false, false)).BuildAndExecute();
            }
            else if (selectedObjects != null)
            {
                _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(selectedObjects, isSelectionModification, removeSelection)).BuildAndExecute();
            }
        }

        void SelectFromPoint(Vector2 mousePosition, bool isSelectionModification, bool removeSelection)
        {
            var ray = _camera.CreateCameraRay(mousePosition);
            var currentState = _selectionManager.GetState();
            if (currentState is FaceSelectionState faceState)
            {
                if (IntersectionMath.IntersectFace(ray, faceState.RenderObject.Geometry, faceState.RenderObject.RenderMatrix, out var selectedFace) != null)
                {
                    _commandFactory.Create<FaceSelectionCommand>().Configure(x => x.Configure(selectedFace.Value, isSelectionModification, removeSelection)).BuildAndExecute();
                    return;
                }
            }

            if (currentState is VertexSelectionState vertexState)
            {
                if (IntersectionMath.IntersectVertex(ray, vertexState.RenderObject.Geometry, _camera.Position, vertexState.RenderObject.RenderMatrix, out var selecteVert) != null)
                {
                    _commandFactory.Create<VertexSelectionCommand>().Configure(x => x.Configure(new List<int>() { selecteVert }, isSelectionModification, removeSelection)).BuildAndExecute();
                    return;
                }
            }

            // Pick object
            var selectedObject = _sceneManger.SelectObject(ray);
            if (selectedObject == null && isSelectionModification == false)
            {
                // Only clear selection if we are not in geometry mode and the selection count is not empty
                if (currentState.Mode != GeometrySelectionMode.Object || currentState.SelectionCount() != 0)
                    _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(new List<ISelectable>(), false, false)).BuildAndExecute();
            }
            else if (selectedObject != null)
            {
                _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(selectedObject, isSelectionModification, removeSelection)).BuildAndExecute();
            }
        }

        public bool SetObjectSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Object)
            {
                _commandFactory.Create<ObjectSelectionModeCommand>().Configure(x => x.Configure(selectionState.GetSingleSelectedObject(), GeometrySelectionMode.Object)).BuildAndExecute();
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
                    _commandFactory.Create<ObjectSelectionModeCommand>().Configure(x => x.Configure(selectedObject, GeometrySelectionMode.Face)).BuildAndExecute();
                    return true;
                }

            }
            return false;
        }

        public bool SetVertexSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Vertex)
            {
                var selectedObject = selectionState.GetSingleSelectedObject();
                if (selectedObject != null)
                {
                    _commandFactory.Create<ObjectSelectionModeCommand>().Configure(x => x.Configure(selectedObject, GeometrySelectionMode.Vertex)).BuildAndExecute();
                    return true;
                }
            }
            return false;
        }

        public bool SetBoneSelectionMode()
        {
            var selectionState = _selectionManager.GetState();
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone)
            {
                var selectedObject = selectionState.GetSingleSelectedObject();
                if (selectedObject != null)
                {
                    _commandFactory.Create<ObjectSelectionModeCommand>().Configure(x => x.Configure(selectedObject, GeometrySelectionMode.Bone)).BuildAndExecute();
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

            else if (_keyboardComponent.IsKeyReleased(Keys.F9))
            {
                if (SetBoneSelectionMode())
                    return true;
            }

            return false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (_isMouseDown)
            {
                var destination = CreateSelectionRectangle(_startDrag, _currentMousePos);
                var lineWidth = 2;
                var top = new Rectangle(destination.X, destination.Y, destination.Width, lineWidth);
                var bottom = new Rectangle(destination.X, destination.Y + destination.Height, destination.Width + 2, lineWidth);
                var left = new Rectangle(destination.X, destination.Y, lineWidth, destination.Height);
                var right = new Rectangle(destination.X + destination.Width, destination.Y, lineWidth, destination.Height);

                _resourceLibrary.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _resourceLibrary.CommonSpriteBatch.Draw(_textTexture, destination, Color.White * 0.5f);
                _resourceLibrary.CommonSpriteBatch.Draw(_textTexture, top, Color.Red * 0.75f);
                _resourceLibrary.CommonSpriteBatch.Draw(_textTexture, bottom, Color.Red * 0.75f);
                _resourceLibrary.CommonSpriteBatch.Draw(_textTexture, left, Color.Red * 0.75f);
                _resourceLibrary.CommonSpriteBatch.Draw(_textTexture, right, Color.Red * 0.75f);
                _resourceLibrary.CommonSpriteBatch.End();
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
            _textTexture.Dispose();
        }
    }
}

