using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using GameWorld.Core.WpfWindow.Internals;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Services;

namespace GameWorld.Core.WpfWindow.Input
{

    /// <summary>
    /// Helper class that converts WPF mouse input to the XNA/MonoGame <see cref="_mouseState"/>.
    /// Required for any WPF hosted control.
    /// </summary>
    public class WpfMouse : IDisposable
    {
        private FrameworkElement _focusElement;

        private MouseState _mouseState;
        private bool _captureMouseWithin = true;

        // Cache window Z-order enumeration (expensive P/Invoke + VisualTree per mouse move)
        private List<Window> _cachedWindowOrder;
        private DateTime _windowOrderCacheTime;
        private static readonly TimeSpan _windowOrderCacheDuration = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Creates a new instance of the mouse helper.
        /// </summary>
        /// <param name="focusElement">The element that will be used as the focus point. Provide your implementation of <see cref="WpfGame"/> here.</param>
        public WpfMouse(IWpfGame focusElement, bool captureMouseWithin)
        {
            if (focusElement == null)
                throw new ArgumentNullException(nameof(focusElement));

            _focusElement = focusElement.GetFocusElement();
            _focusElement.MouseWheel += HandleMouse;
            // movement
            _focusElement.MouseMove += HandleMouse;
            _focusElement.MouseEnter += HandleMouse;
            _focusElement.MouseLeave += HandleMouse;
            // clicks
            _focusElement.MouseLeftButtonDown += HandleMouse;
            _focusElement.MouseLeftButtonUp += HandleMouse;
            _focusElement.MouseRightButtonDown += HandleMouse;
            _focusElement.MouseRightButtonUp += HandleMouse;

            _captureMouseWithin = captureMouseWithin;
        }


        /// <summary>
        /// Gets or sets the mouse capture behaviour.
        /// If true, the mouse will be captured within the control. This means that the control will still capture mouse events when the user drags the mouse outside the control.
        /// E.g. mouse down on game window, mouse drag to outside of the window, mouse release -> the game will still register the mouse release. The downside is that overlayed elements (textbox, etc.) will never be able to receive focus.
        /// If false, mouse events outside the game window are never registered. E.g. mouse down on game window, mouse drag to outside of the window, mouse release -> the game will still thing the mouse is pressed until the cursor enters the window again.
        /// The upside is that overlayed controls (e.g. textboxes) can receive focus and input.
        /// Defaults to true.
        /// </summary>
        public bool CaptureMouseWithin
        {
            get { return _captureMouseWithin; }
            set
            {
                if (!value)
                {
                    if (_focusElement.IsMouseCaptured)
                    {
                        _focusElement.ReleaseMouseCapture();
                    }
                }
                _captureMouseWithin = value;
            }
        }
        public MouseState GetState() => _mouseState;
        private void HandleMouse(object sender, MouseEventArgs e)
        {
            if (e.Handled)
                return;

            // Detect if there is a window that is on top of the main application. If so, we dont want to capture the mouse
            // Cache the window enumeration to avoid P/Invoke + VisualTree on every mouse move
            GetCursorPos(out var p);
            var now = DateTime.UtcNow;
            if (_cachedWindowOrder == null || (now - _windowOrderCacheTime) > _windowOrderCacheDuration)
            {
                _cachedWindowOrder = SortWindowsTopToBottom(Application.Current.Windows.OfType<Window>()).ToList();
                _windowOrderCacheTime = now;
            }
            var topToBottom = _cachedWindowOrder;
            foreach (var window in topToBottom)
            {
                var hitPoint = window.PointFromScreen(new Point() { X = p.X, Y = p.Y });
                var hitResult = VisualTreeHelper.HitTest(window, hitPoint);
                if (hitResult?.VisualHit != null)
                {
                    if (window.ToString() != "AssetEditor.Views.MainWindow")
                    {
                        _focusElement.ReleaseMouseCapture();
                        return;
                    }
                }
            }

            var pos = e.GetPosition(_focusElement);
            if (_focusElement.IsMouseDirectlyOver && System.Windows.Input.Keyboard.FocusedElement != _focusElement)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var res = LogicalTreeHelperEx.FindParent<Grid>(_focusElement);
                    if (res != null)
                    {
                        var result = VisualTreeHelper.HitTest(res, pos);
                        if (result?.VisualHit == _focusElement)
                        {
                            _focusElement.Focus();
                        }
                    }
                }
            }

            if ((!_focusElement.IsMouseDirectlyOver || _focusElement.IsMouseCaptured) && CaptureMouseWithin)
            {
                var v = (Visual)_focusElement;
                var hit = false;
                var res = LogicalTreeHelperEx.FindParent<Grid>(_focusElement);
                //if (res == null) return; <-- please see: https://github.com/donkeyProgramming/TheAssetEditor/pull/90#:~:text=Monogame.WpfInterop/Input/WpfMouse.cs
                if (res != null)
                {
                    var result = VisualTreeHelper.HitTest(res, pos);
                    if (result?.VisualHit == _focusElement)
                        hit = true;
                }

                if (!hit)
                {
                    if (_focusElement.IsMouseCaptured)
                    {
                        _mouseState = new MouseState(_mouseState.X, _mouseState.Y, _mouseState.ScrollWheelValue,
                            (ButtonState)e.LeftButton, (ButtonState)e.MiddleButton, (ButtonState)e.RightButton, (ButtonState)e.XButton1,
                            (ButtonState)e.XButton2);
                        // only release if LeftMouse is up
                        if (e.LeftButton == MouseButtonState.Released)
                        {
                            _focusElement.ReleaseMouseCapture();
                        }
                        e.Handled = true;
                    }

                    // mouse is outside the control and not captured, so don't update the mouse state
                    return;
                }
            }

            if (!_focusElement.IsMouseCaptured)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _focusElement.CaptureMouse();
                }

            }

            e.Handled = true;
            var m = _mouseState;
            var w = e as MouseWheelEventArgs;
            _mouseState = new MouseState((int)pos.X, (int)pos.Y, m.ScrollWheelValue + (w?.Delta ?? 0), (ButtonState)e.LeftButton, (ButtonState)e.MiddleButton, (ButtonState)e.RightButton, (ButtonState)e.XButton1, (ButtonState)e.XButton2);
        }

        private static double Clamp(double v, int min, double max)
        {
            return v < min ? min : v > max ? max : v;
        }

        /// <summary>
        /// Sets the cursor to the specific coordinates within the attached game.
        /// This is required as the monogame Mouse.SetPosition function relies on the underlying Winforms implementation and will not work with WPF.
        /// Also updates the internal mouse state immediately to ensure correct position in next frame.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetCursor(int x, int y)
        {
            var p = _focusElement.PointToScreen(new Point(x, y));
            SetCursorPos((int)p.X, (int)p.Y);

            // Immediately update mouse state to prevent stale position in next frame
            // This is critical for infinite drag to work smoothly at high frame rates
            var m = _mouseState;
            _mouseState = new MouseState(x, y, m.ScrollWheelValue, m.LeftButton, m.MiddleButton, m.RightButton, m.XButton1, m.XButton2);
        }

        /// <summary>
        /// Sets the mouse cursor appearance for modal transform operations.
        /// </summary>
        /// <param name="cursor">The cursor type to display, or null to reset to default.</param>
        public void SetCursorType(System.Windows.Input.Cursor cursor)
        {
            if (_focusElement != null)
            {
                if (cursor == null)
                {
                    _focusElement.Cursor = System.Windows.Input.Cursors.Arrow;
                    System.Windows.Input.Mouse.OverrideCursor = null;
                }
                else
                {
                    _focusElement.Cursor = cursor;
                    System.Windows.Input.Mouse.OverrideCursor = cursor;
                }
            }
        }

        /// <summary>
        /// Resets the mouse cursor to the default arrow.
        /// </summary>
        public void ResetCursor()
        {
            SetCursorType(null);
        }

        /// <summary>
        /// Get the size of the focus element (viewport size)
        /// </summary>
        public Microsoft.Xna.Framework.Vector2 GetElementSize()
        {
            if (_focusElement != null)
            {
                return new Microsoft.Xna.Framework.Vector2((float)_focusElement.ActualWidth, (float)_focusElement.ActualHeight);
            }
            return Microsoft.Xna.Framework.Vector2.Zero;
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }


        const uint GW_HWNDNEXT = 2;
        [DllImport("User32")] static extern nint GetTopWindow(nint hWnd);
        [DllImport("User32")] static extern nint GetWindow(nint hWnd, uint wCmd);

        public IEnumerable<Window> SortWindowsTopToBottom(IEnumerable<Window> unsorted)
        {
            var byHandle = unsorted.Select(win =>
            {
                var a = PresentationSource.FromVisual(win);
                var s = (HwndSource)a;
                return (win, s?.Handle);
            })
                .Where(x => x.Handle != null)
                .Where(x => x.win.ToString() != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners.AdornerWindow")
                .ToDictionary(x => x.Handle);


            for (var hWnd = GetTopWindow(nint.Zero); hWnd != nint.Zero; hWnd = GetWindow(hWnd, GW_HWNDNEXT))
            {
                if (byHandle.ContainsKey(hWnd))
                    yield return byHandle[hWnd].win;
            }
        }

        public void Dispose()
        {
            _focusElement.MouseWheel -= HandleMouse;
            // movement
            _focusElement.MouseMove -= HandleMouse;
            _focusElement.MouseEnter -= HandleMouse;
            _focusElement.MouseLeave -= HandleMouse;
            // clicks
            _focusElement.MouseLeftButtonDown -= HandleMouse;
            _focusElement.MouseLeftButtonUp -= HandleMouse;
            _focusElement.MouseRightButtonDown -= HandleMouse;
            _focusElement.MouseRightButtonUp -= HandleMouse;
            _focusElement = null;
        }
    }
}
