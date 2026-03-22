using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Gizmo
{
    public class BlenderTransformInputHandler
    {
        #region Win32 API 导入
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; }
        #endregion

        public Action OnUndoRequested;

        private string _interactMode = "None";
        private string _lockAxis = "None";
        private bool _isCursorHidden = false;

        private Win32Point _initialMousePos;
        private Win32Point _lastMousePos;
        private double _virtualDeltaX = 0;
        private double _virtualDeltaY = 0;

        private readonly uint _currentProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        private const float BASE_SENS_TRANS = 0.005f;
        private const float BASE_SENS_ROT = 0.01f;

        private bool _waitForLmbRelease = false;
        private bool _wasUndoPressed = false;
        private bool _ignoreNextDelta = false;

        private void HideCursor() { if (!_isCursorHidden) { ShowCursor(false); _isCursorHidden = true; } }
        private void RestoreCursor() { if (_isCursorHidden) { ShowCursor(true); _isCursorHidden = false; } }

        public bool HandleInput(TransformGizmoWrapper wrapper, ArcBallCamera camera, CommandExecutor commandManager)
        {
            if (Keyboard.FocusedElement is TextBox) return false;

            // [FIX] 强力防死锁拦截：没有有效物体被选中时，无论按什么键，全部放行不吞没！
            if (wrapper == null || !wrapper.HasValidTarget) return false;

            IntPtr fgWindow = GetForegroundWindow();
            GetWindowThreadProcessId(fgWindow, out uint activeProcId);
            if (activeProcId != _currentProcessId)
            {
                if (_interactMode != "None") CancelInteraction(wrapper, commandManager);
                return false;
            }

            bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;
            bool zPressed = (GetAsyncKeyState(0x5A) & 0x8000) != 0;
            bool xPressed = (GetAsyncKeyState(0x58) & 0x8000) != 0;
            bool yPressed = (GetAsyncKeyState(0x59) & 0x8000) != 0;
            bool gPressed = (GetAsyncKeyState(0x47) & 0x8000) != 0;
            bool rPressed = (GetAsyncKeyState(0x52) & 0x8000) != 0;
            bool escPressed = (GetAsyncKeyState(0x1B) & 0x8000) != 0;
            bool lmbPressed = (GetAsyncKeyState(0x01) & 0x8000) != 0;
            bool rmbPressed = (GetAsyncKeyState(0x02) & 0x8000) != 0;

            bool isUndoPressingNow = ctrlPressed && zPressed;
            if (isUndoPressingNow && !_wasUndoPressed)
            {
                if (_interactMode != "None") CancelInteraction(wrapper, commandManager);
                OnUndoRequested?.Invoke();
                _wasUndoPressed = true;
                return true;
            }
            else if (!isUndoPressingNow) _wasUndoPressed = false;

            if (_interactMode == "None")
            {
                if ((gPressed || rPressed) && !ctrlPressed)
                {
                    _interactMode = gPressed ? "Translate" : "Rotate";
                    _lockAxis = "None";
                    _waitForLmbRelease = lmbPressed;

                    GetCursorPos(ref _lastMousePos);
                    _initialMousePos = _lastMousePos;
                    _virtualDeltaX = 0;
                    _virtualDeltaY = 0;
                    _ignoreNextDelta = false;

                    HideCursor();
                    wrapper.Start(commandManager);
                    return true;
                }
                return false;
            }
            else
            {
                if (_waitForLmbRelease) { if (!lmbPressed) _waitForLmbRelease = false; }
                else { if (lmbPressed) { ConfirmInteraction(wrapper, commandManager); return true; } }

                if (rmbPressed || escPressed) { CancelInteraction(wrapper, commandManager); return true; }

                if (xPressed) _lockAxis = "X";
                if (yPressed) _lockAxis = "Y";
                if (zPressed && !ctrlPressed) _lockAxis = "Z";

                Win32Point pt = new Win32Point();
                GetCursorPos(ref pt);

                if (_ignoreNextDelta)
                {
                    _lastMousePos = pt;
                    _ignoreNextDelta = false;
                    return true;
                }

                double frameDeltaX = pt.X - _lastMousePos.X;
                double frameDeltaY = pt.Y - _lastMousePos.Y;
                _lastMousePos = pt;

                if (Math.Abs(pt.X - _initialMousePos.X) > 400 || Math.Abs(pt.Y - _initialMousePos.Y) > 400)
                {
                    SetCursorPos(_initialMousePos.X, _initialMousePos.Y);
                    _ignoreNextDelta = true;
                }

                if (frameDeltaX == 0 && frameDeltaY == 0) return true;

                double accelerationMult = shiftPressed ? 0.1 : 1.0;
                _virtualDeltaX += frameDeltaX * accelerationMult;
                _virtualDeltaY += frameDeltaY * accelerationMult;

                Matrix view = camera.ViewMatrix;
                Vector3 camRight = new Vector3(view.M11, view.M21, view.M31);
                Vector3 camUp = new Vector3(view.M12, view.M22, view.M32);

                Vector3 worldDeltaPos = Vector3.Zero;
                Quaternion worldDeltaRot = Quaternion.Identity;

                Vector2 rotMult = wrapper.ActiveTarget?.RotationMultiplier ?? new Vector2(-1f, 1f);

                float transX = (float)_virtualDeltaX * BASE_SENS_TRANS;
                float transY = (float)_virtualDeltaY * BASE_SENS_TRANS;
                float rotX = (float)_virtualDeltaX * BASE_SENS_ROT * rotMult.X;
                float rotY = (float)_virtualDeltaY * BASE_SENS_ROT * rotMult.Y;

                if (_interactMode == "Translate")
                {
                    if (_lockAxis == "X") worldDeltaPos = new Vector3(transX, 0, 0);
                    else if (_lockAxis == "Y") worldDeltaPos = new Vector3(0, -transY, 0);
                    else if (_lockAxis == "Z") worldDeltaPos = new Vector3(0, 0, transX);
                    else worldDeltaPos = (-camRight * transX) - (camUp * transY);
                }
                else if (_interactMode == "Rotate")
                {
                    if (_lockAxis == "X") worldDeltaRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rotX);
                    else if (_lockAxis == "Y") worldDeltaRot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotX);
                    else if (_lockAxis == "Z") worldDeltaRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotX);
                    else
                    {
                        Quaternion yaw = Quaternion.CreateFromAxisAngle(camUp, rotX);
                        Quaternion pitch = Quaternion.CreateFromAxisAngle(camRight, rotY);
                        worldDeltaRot = yaw * pitch;
                    }
                }

                wrapper.ApplyTransformDelta(worldDeltaPos, worldDeltaRot);
                return true;
            }
        }

        private void ConfirmInteraction(TransformGizmoWrapper wrapper, CommandExecutor commandManager)
        {
            RestoreCursor();
            _interactMode = "None";
            wrapper.Stop(commandManager);
        }

        private void CancelInteraction(TransformGizmoWrapper wrapper, CommandExecutor commandManager)
        {
            RestoreCursor();
            _interactMode = "None";
            wrapper.ApplyTransformDelta(Vector3.Zero, Quaternion.Identity);
            wrapper.Stop(commandManager);
        }
    }
}
