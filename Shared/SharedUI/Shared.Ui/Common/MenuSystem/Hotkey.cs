// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace Shared.Ui.Common.MenuSystem
{
    public class Hotkey
    {
        public ModifierKeys ModifierKeys { get; set; }
        public Key Key { get; set; }

        public Hotkey(Key key, ModifierKeys modifierKeys)
        {
            Key = key;
            ModifierKeys = modifierKeys;
        }
    }
}
