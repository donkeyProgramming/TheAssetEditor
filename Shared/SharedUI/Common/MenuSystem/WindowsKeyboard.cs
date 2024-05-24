// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Input;

namespace Shared.Ui.Common.MenuSystem
{
    public interface IKeyboardHandler
    {
        bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys);
        void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers);
    }

    public class WindowKeyboard
    {
        Dictionary<Key, bool> _isKeyDown = new Dictionary<Key, bool>();
        public bool IsKeyDown(Key key)
        {
            _isKeyDown.TryGetValue(key, out var value);
            return value;
        }

        public void SetKeyDown(Key key, bool status)
        {
            _isKeyDown[key] = status;
        }
    }
}
