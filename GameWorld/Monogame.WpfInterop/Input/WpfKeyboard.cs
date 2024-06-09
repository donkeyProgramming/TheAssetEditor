﻿using GameWorld.WpfWindow;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Keyboard = System.Windows.Input.Keyboard;

namespace MonoGame.Framework.WpfInterop.Input
{
    /// <summary>
    /// Helper class that accesses a native API to get the current keystate.
    /// Required for any WPF hosted control.
    /// </summary>
    public class WpfKeyboard
    {
        private readonly WpfGame _focusElement;

        /// <summary>
        /// Creates a new instance of the keyboard helper.
        /// </summary>
        /// <param name="focusElement">The element that will be used as the focus point. Provide your implementation of <see cref="WpfGame"/> here.</param>
        public WpfKeyboard(WpfGame focusElement)
        {
            if (focusElement == null)
                throw new ArgumentNullException(nameof(focusElement));

            _focusElement = focusElement;
        }

        /// <summary>
        /// Gets the active keyboardstate.
        /// </summary>
        /// <returns></returns>
        public KeyboardState GetState()
        {
            if (_focusElement.IsMouseDirectlyOver && Keyboard.FocusedElement != _focusElement)
            {
                if (System.Windows.Input.Mouse.Captured == _focusElement)
                    _focusElement.Focus();
            }
            return new KeyboardState(GetKeys(_focusElement));
        }

        private static Keys[] GetKeys(IInputElement focusElement)
        {
            // the buffer must be exactly 256 bytes long as per API definition
            var keyStates = new byte[256];

            if (!NativeGetKeyboardState(keyStates))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pressedKeys = new List<Keys>();
            if (focusElement.IsKeyboardFocused)
            {
                // skip the first 8 entries as they are actually mouse events and not keyboard keys
                const int skipMouseKeys = 8;
                for (int i = skipMouseKeys; i < keyStates.Length; i++)
                {
                    byte key = keyStates[i];

                    //Logical 'and' so we can drop the low-order bit for toggled keys, else that key will appear with the value 1!
                    if ((key & 0x80) != 0)
                    {

                        //This is just for a short demo, you may want this to return
                        //multiple keys!
                        if (key != 0)
                            pressedKeys.Add((Keys)i);
                    }
                }
            }
            return pressedKeys.ToArray();
        }

        [DllImport("user32.dll", EntryPoint = "GetKeyboardState", SetLastError = true)]
        private static extern bool NativeGetKeyboardState([Out] byte[] keyStates);
    }
}
