using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Services;
using GameWorld.Core.Services;
using Shared.GameFormats.AnimationMeta.Parsing;
using GameWorld.Core.Components.Input;
using GameWorld.Core.SceneNodes;

namespace Editors.AnimationMeta.SuperView
{
    public delegate void DragUpdateDelegate(Vector3 worldDeltaPos, Quaternion worldDeltaRot);

    public class SuperViewManipulatorComponent : IGameComponent, IUpdateable
    {
        public enum ManipulateMode { None, Move, Rotate }
        public enum AxisLock { None, X, Y, Z }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        public struct POINT { public int X; public int Y; }

        private class UndoRecord
        {
            public Vector3? Pos; public Vector4? Rot;
            public Vector3? StartPos; public Vector3? EndPos;
        }
        private readonly Stack<UndoRecord> _undoStack = new Stack<UndoRecord>();

        private readonly IWpfGame _game;
        private readonly FocusSelectableObjectService _cameraService;
        private readonly IKeyboardComponent _keyboard;
        private readonly IMouseComponent _mouse;

        public ParsedMetadataAttribute SelectedAttribute { get; set; }

        public Func<SceneNode> GetSelectedNode { get; set; }
        public SceneNode SelectedNode { get; private set; }

        public ManipulateMode CurrentMode { get; private set; } = ManipulateMode.None;
        public AxisLock CurrentAxis { get; private set; } = AxisLock.None;

        private Vector3 _originalLocalPos;
        private Quaternion _originalLocalRot;
        private Vector3 _originalLocalStartPos;
        private Vector3 _originalLocalEndPos;
        private bool _isSplashAttack = false;

        // 【核心解算引擎】
        public Func<Matrix> GetBoneWorldMatrix { get; set; }
        private Quaternion _boneWorldRotation;
        public Vector3 TrueWorldPivot { get; private set; }

        private Vector3 _startIntersect;
        private Vector2 _startMousePos;
        private POINT _lockedPhysicalCursorPos;
        private Vector2 _virtualMousePos;

        public event Action OnDragStarted;
        public event DragUpdateDelegate OnDragUpdate;
        public event Action OnDragCompleted;

        public bool Enabled { get; set; } = true;
        public int UpdateOrder { get; set; } = 1000;
        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public SuperViewManipulatorComponent(IWpfGame game, FocusSelectableObjectService cameraService, IKeyboardComponent keyboard, IMouseComponent mouse)
        {
            _game = game;
            _cameraService = cameraService;
            _keyboard = keyboard;
            _mouse = mouse;
        }

        public void Initialize() { }

        private bool HasSpatialProperty(object item)
        {
            if (item == null) return false;
            var type = item.GetType();
            return type.GetProperty("Position") != null ||
                   type.GetProperty("StartPosition") != null ||
                   type.GetProperty("Orientation") != null;
        }

        public void Update(GameTime gameTime)
        {
            if (_mouse.IsMouseButtonPressed(MouseButton.Left) || _mouse.IsMouseButtonPressed(MouseButton.Right))
                _game.GetFocusElement()?.Focus();

            if (_keyboard.IsKeyDown(Keys.LeftControl) && _keyboard.IsKeyReleased(Keys.Z))
            {
                if (CurrentMode == ManipulateMode.None && _undoStack.Count > 0)
                {
                    var record = _undoStack.Pop();
                    ApplyRawData(record.Pos, record.Rot, record.StartPos, record.EndPos);
                    OnDragCompleted?.Invoke();
                }
            }

            if (SelectedAttribute == null) return;
            if (_mouse.MouseOwner != null && _mouse.MouseOwner != this) return;

            bool shiftDown = _keyboard.IsKeyDown(Keys.LeftShift) || _keyboard.IsKeyDown(Keys.RightShift);
            float speedMultiplier = shiftDown ? 0.1f : 1.0f;

            if (CurrentMode == ManipulateMode.None)
            {
                if (_keyboard.IsKeyDown(Keys.G)) StartManipulation(ManipulateMode.Move, _mouse.Position());
                else if (_keyboard.IsKeyDown(Keys.R)) StartManipulation(ManipulateMode.Rotate, _mouse.Position());
                return;
            }

            if (_mouse.MouseOwner == null) _mouse.MouseOwner = this;

            if (_keyboard.IsKeyDown(Keys.X)) CurrentAxis = AxisLock.X;
            else if (_keyboard.IsKeyDown(Keys.Y)) CurrentAxis = AxisLock.Y;
            else if (_keyboard.IsKeyDown(Keys.Z)) CurrentAxis = AxisLock.Z;

            GetCursorPos(out POINT currentPhysicalPos);
            int dx = currentPhysicalPos.X - _lockedPhysicalCursorPos.X;
            int dy = currentPhysicalPos.Y - _lockedPhysicalCursorPos.Y;

            if (dx != 0 || dy != 0)
            {
                _virtualMousePos.X += dx;
                _virtualMousePos.Y += dy;
                SetCursorPos(_lockedPhysicalCursorPos.X, _lockedPhysicalCursorPos.Y);
            }

            if (CurrentMode == ManipulateMode.Move) UpdateMovement(_virtualMousePos, speedMultiplier);
            else if (CurrentMode == ManipulateMode.Rotate) UpdateRotation(_virtualMousePos, speedMultiplier);

            if (_mouse.IsMouseButtonPressed(MouseButton.Left))
            {
                CurrentMode = ManipulateMode.None;
                _mouse.MouseOwner = null;
                ShowAndReleaseMouse();
                OnDragCompleted?.Invoke();
            }
            else if (_mouse.IsMouseButtonPressed(MouseButton.Right) || _keyboard.IsKeyDown(Keys.Escape))
            {
                CurrentMode = ManipulateMode.None;
                _mouse.MouseOwner = null;
                ShowAndReleaseMouse();

                if (_undoStack.Count > 0)
                {
                    var record = _undoStack.Pop();
                    ApplyRawData(record.Pos, record.Rot, record.StartPos, record.EndPos);
                }

                OnDragUpdate?.Invoke(Vector3.Zero, Quaternion.Identity);
                OnDragCompleted?.Invoke();
            }
        }

        private void StartManipulation(ManipulateMode mode, Vector2 mousePos)
        {
            if (!HasSpatialProperty(SelectedAttribute)) return;

            CurrentMode = mode;
            CurrentAxis = AxisLock.None;
            _isSplashAttack = false;

            dynamic meta = SelectedAttribute;

            try { _originalLocalPos = meta.Position; } catch { _originalLocalPos = Vector3.Zero; }
            try { _originalLocalRot = new Quaternion(meta.Orientation); } catch { _originalLocalRot = Quaternion.Identity; }

            try
            {
                _originalLocalStartPos = meta.StartPosition;
                _originalLocalEndPos = meta.EndPosition;
                _originalLocalPos = _originalLocalStartPos;
                _isSplashAttack = true;
            }
            catch { }

            _undoStack.Push(new UndoRecord
            {
                Pos = _originalLocalPos,
                Rot = new Vector4(_originalLocalRot.X, _originalLocalRot.Y, _originalLocalRot.Z, _originalLocalRot.W),
                StartPos = _isSplashAttack ? _originalLocalStartPos : null,
                EndPos = _isSplashAttack ? _originalLocalEndPos : null
            });

            SelectedNode = GetSelectedNode?.Invoke();

            // 【FIX】: Directly get the real bone world matrix to avoid coordinate offset
            Matrix boneMatrix = GetBoneWorldMatrix != null ? GetBoneWorldMatrix() : Matrix.Identity;

            if (boneMatrix == Matrix.Identity)
            {
                TrueWorldPivot = _originalLocalPos;
                _boneWorldRotation = Quaternion.Identity;
            }
            else
            {
                boneMatrix.Decompose(out _, out Quaternion boneRot, out _);
                _boneWorldRotation = boneRot;
                TrueWorldPivot = Vector3.Transform(_originalLocalPos, boneMatrix);
            }

            _startMousePos = mousePos;
            _virtualMousePos = mousePos;

            GetCursorPos(out _lockedPhysicalCursorPos);
            HideAndLockMouse();

            _startIntersect = GetMousePlaneIntersection(_virtualMousePos, TrueWorldPivot, GetWorkPlaneNormal());
            OnDragStarted?.Invoke();
        }

        private void UpdateMovement(Vector2 virtualMousePos, float speed)
        {
            var planeNormal = GetWorkPlaneNormal();
            var currentIntersect = GetMousePlaneIntersection(virtualMousePos, TrueWorldPivot, planeNormal);
            var worldDelta = (currentIntersect - _startIntersect) * speed;

            if (CurrentAxis == AxisLock.X) worldDelta *= new Vector3(1, 0, 0);
            if (CurrentAxis == AxisLock.Y) worldDelta *= new Vector3(0, 1, 0);
            if (CurrentAxis == AxisLock.Z) worldDelta *= new Vector3(0, 0, 1);

            Vector3 localDelta = Vector3.Transform(worldDelta, Quaternion.Inverse(_boneWorldRotation));

            if (_isSplashAttack)
                ApplyRawData(null, null, _originalLocalStartPos + localDelta, _originalLocalEndPos + localDelta);
            else
                ApplyRawData(_originalLocalPos + localDelta, null, null, null);

            OnDragUpdate?.Invoke(worldDelta, Quaternion.Identity);
        }

        private void UpdateRotation(Vector2 virtualMousePos, float speed)
        {
            Vector3 projectedVec = _game.GraphicsDevice.Viewport.Project(
                TrueWorldPivot, _cameraService.Camera.ProjectionMatrix, _cameraService.Camera.ViewMatrix, Matrix.Identity);

            Vector2 screenCenter = new Vector2(projectedVec.X, projectedVec.Y);
            Vector2 startVec = _startMousePos - screenCenter;
            Vector2 currentVec = virtualMousePos - screenCenter;

            if (startVec.LengthSquared() < 1 || currentVec.LengthSquared() < 1) return;

            startVec.Normalize();
            currentVec.Normalize();

            float angle = (float)Math.Atan2(
                startVec.X * currentVec.Y - startVec.Y * currentVec.X,
                startVec.X * currentVec.X + startVec.Y * currentVec.Y);

            // 【FIX】: Inverted angle calculation to fix reverse rotation mapping
            angle *= -speed;

            Vector3 cameraForward = Vector3.Normalize(_cameraService.Camera.LookAt - _cameraService.Camera.Position);
            Vector3 rotAxis = cameraForward;

            if (CurrentAxis == AxisLock.X) rotAxis = Vector3.UnitX;
            if (CurrentAxis == AxisLock.Y) rotAxis = Vector3.UnitY;
            if (CurrentAxis == AxisLock.Z) rotAxis = Vector3.UnitZ;

            Quaternion worldDeltaRot = Quaternion.CreateFromAxisAngle(rotAxis, angle);
            Quaternion localDeltaRot = Quaternion.Inverse(_boneWorldRotation) * worldDeltaRot * _boneWorldRotation;

            if (_isSplashAttack)
            {
                Vector3 newEndLocal = _originalLocalStartPos + Vector3.Transform(_originalLocalEndPos - _originalLocalStartPos, localDeltaRot);
                ApplyRawData(null, null, _originalLocalStartPos, newEndLocal);
            }
            else
            {
                Quaternion newLocalRot = localDeltaRot * _originalLocalRot;
                ApplyRawData(null, new Vector4(newLocalRot.X, newLocalRot.Y, newLocalRot.Z, newLocalRot.W), null, null);
            }

            OnDragUpdate?.Invoke(Vector3.Zero, worldDeltaRot);
        }

        private void ApplyRawData(Vector3? pos, Vector4? rot, Vector3? startPos, Vector3? endPos)
        {
            try
            {
                dynamic meta = SelectedAttribute;
                if (pos.HasValue) try { meta.Position = pos.Value; } catch { }
                if (rot.HasValue) try { meta.Orientation = rot.Value; } catch { }
                if (startPos.HasValue) try { meta.StartPosition = startPos.Value; } catch { }
                if (endPos.HasValue) try { meta.EndPosition = endPos.Value; } catch { }
            }
            catch { }
        }

        private void HideAndLockMouse()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    var element = _game.GetFocusElement();
                    if (element != null)
                    {
                        System.Windows.Input.Mouse.Capture(element);
                        element.Cursor = System.Windows.Input.Cursors.None;
                    }
                });
            }
            catch { }
        }

        private void ShowAndReleaseMouse()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    var element = _game.GetFocusElement();
                    if (element != null)
                    {
                        element.ReleaseMouseCapture();
                        element.Cursor = System.Windows.Input.Cursors.Arrow;
                    }
                });
            }
            catch { }
        }

        private Vector3 GetMousePlaneIntersection(Vector2 mousePos, Vector3 planePoint, Vector3 planeNormal)
        {
            var viewport = _game.GraphicsDevice.Viewport;
            var nearPoint = viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 0), _cameraService.Camera.ProjectionMatrix, _cameraService.Camera.ViewMatrix, Matrix.Identity);
            var farPoint = viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), _cameraService.Camera.ProjectionMatrix, _cameraService.Camera.ViewMatrix, Matrix.Identity);

            var rayDir = farPoint - nearPoint;
            rayDir.Normalize();

            float denominator = Vector3.Dot(planeNormal, rayDir);
            if (Math.Abs(denominator) > 0.0001f)
            {
                float t = Vector3.Dot(planePoint - nearPoint, planeNormal) / denominator;
                return nearPoint + rayDir * t;
            }
            return planePoint;
        }

        private Vector3 GetWorkPlaneNormal()
        {
            if (CurrentAxis == AxisLock.X) return Vector3.UnitZ;
            if (CurrentAxis == AxisLock.Y) return Vector3.UnitZ;
            if (CurrentAxis == AxisLock.Z) return Vector3.UnitX;
            return Vector3.Normalize(_cameraService.Camera.LookAt - _cameraService.Camera.Position);
        }
    }
}
