using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using View3D.Commands;
using View3D.Commands.Bone;
using View3D.Commands.Face;
using View3D.Commands.Object;
using View3D.Commands.Vertex;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Utility;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using MouseButton = View3D.Components.Input.MouseButton;

namespace View3D.Components.Component.Selection
{
    public class SelectionComponent : BaseComponent, IDisposable
    {
        SpriteBatch _spriteBatch;
        Texture2D _textTexture;

        KeyboardComponent _keyboardComponent;
        MouseComponent _mouseComponent;
        ArcBallCamera _camera;

        SelectionManager _selectionManager;

        private readonly DeviceResolverComponent _deviceResolverComponent;
        private readonly CommandFactory _commandFactory;
        SceneManager _sceneManger;


        bool _isMouseDown = false;
        Vector2 _startDrag;
        Vector2 _currentMousePos;

        public SelectionComponent(
            MouseComponent mouseComponent, KeyboardComponent keyboardComponent,
            ArcBallCamera camera, SelectionManager selectionManager,
            DeviceResolverComponent deviceResolverComponent, CommandFactory commandFactory,
            SceneManager sceneManager)
        {
            _mouseComponent = mouseComponent;
            _keyboardComponent = keyboardComponent;
            _camera = camera;
            _selectionManager = selectionManager;
            _deviceResolverComponent = deviceResolverComponent;
            _commandFactory = commandFactory;
            _sceneManger = sceneManager;
        }

        public override void Initialize()
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.SelectionComponent;
            DrawOrder = (int)ComponentDrawOrderEnum.SelectionComponent;

            _spriteBatch = new SpriteBatch(_deviceResolverComponent.Device);
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
                    System.Windows.Forms.MessageBox.Show("no object was selected. select an object first then you can select the bone", "no object", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (boneState.CurrentAnimation == null)
                {
                    System.Windows.Forms.MessageBox.Show("no animation was played. select a frame by scrubbing the animation using the control below", "no animation", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

