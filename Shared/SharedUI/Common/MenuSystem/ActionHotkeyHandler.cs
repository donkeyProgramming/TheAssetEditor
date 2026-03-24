// File: Shared/SharedUI/Common/MenuSystem/ActionHotkeyHandler.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Ui.Common.MenuSystem
{
    public class ActionHotkeyHandler
    {
        List<MenuAction> _actions = new List<MenuAction>();

        // [FIX] Cache the owner editor index
        private int _ownerEditorIndex = -1;
        private bool _isOwnerIdentified = false;

        public void Register(MenuAction action)
        {
            _actions.Add(action);
        }

        public bool TriggerCommand(Key key, ModifierKeys modifierKeys)
        {
            var isHandled = false;

            // [FIX] Identify owner editor on first trigger
            if (!_isOwnerIdentified)
            {
                IdentifyOwnerEditor();
                _isOwnerIdentified = true;
            }

            // [FIX] Only handle if this is the currently selected editor
            if (!IsCurrentEditor())
                return false;

            foreach (var item in _actions)
            {
                if (item.Hotkey == null)
                    continue;

                if (item.Hotkey.Key == key && item.Hotkey.ModifierKeys == modifierKeys)
                {
                    item.TriggerAction();
                    isHandled = true;
                }
            }

            return isHandled;
        }

        /// <summary>
        /// Identifies the owner editor by finding the index in EditorManager.CurrentEditorsList.
        /// </summary>
        private void IdentifyOwnerEditor()
        {
            try
            {
                var editorManager = GetEditorManager();
                if (editorManager == null)
                    return;

                // Get CurrentEditorsList
                var editorsListProperty = editorManager.GetType().GetProperty("CurrentEditorsList");
                if (editorsListProperty == null)
                    return;

                var editorsList = editorsListProperty.GetValue(editorManager) as System.Collections.IList;
                if (editorsList == null)
                    return;

                // Find the editor that owns this ActionHotkeyHandler
                for (int i = 0; i < editorsList.Count; i++)
                {
                    var editor = editorsList[i];

                    // Check if this editor has a MenuBar property
                    var menuBarProperty = editor.GetType().GetProperty("MenuBar");
                    if (menuBarProperty == null)
                        continue;

                    var menuBar = menuBarProperty.GetValue(editor);
                    if (menuBar == null)
                        continue;

                    // Check if the MenuBar has the same hotkey handler instance
                    var hotKeyHandlerField = menuBar.GetType().GetField("_hotKeyHandler",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (hotKeyHandlerField != null)
                    {
                        var handler = hotKeyHandlerField.GetValue(menuBar);
                        if (handler == this)
                        {
                            _ownerEditorIndex = i;
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Gets the EditorManager instance from the service provider.
        /// </summary>
        private object GetEditorManager()
        {
            try
            {
                if (Application.Current == null)
                    return null;

                // Try to get from App.xaml.cs ServiceProvider
                var app = Application.Current;
                var serviceProviderProperty = app.GetType().GetProperty("ServiceProvider");
                if (serviceProviderProperty != null)
                {
                    var serviceProvider = serviceProviderProperty.GetValue(app) as IServiceProvider;
                    if (serviceProvider != null)
                    {
                        // Get IEditorManager
                        var editorManagerType = Type.GetType("Shared.Core.ToolCreation.IEditorManager, Shared.Core");
                        if (editorManagerType != null)
                        {
                            return serviceProvider.GetService(editorManagerType);
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        /// <summary>
        /// Checks if this ActionHotkeyHandler belongs to the currently selected editor.
        /// </summary>
        private bool IsCurrentEditor()
        {
            // If not identified, allow the action (fallback)
            if (_ownerEditorIndex < 0)
                return true;

            try
            {
                var editorManager = GetEditorManager();
                if (editorManager == null)
                    return true;

                // Get SelectedEditorIndex
                var selectedIndexProperty = editorManager.GetType().GetProperty("SelectedEditorIndex");
                if (selectedIndexProperty == null)
                    return true;

                var selectedIndex = (int)selectedIndexProperty.GetValue(editorManager);

                // [KEY FIX] Only handle if this is the currently selected editor
                return selectedIndex == _ownerEditorIndex;
            }
            catch
            {
                return true;
            }
        }
    }
}
