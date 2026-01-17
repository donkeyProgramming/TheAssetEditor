using System.Windows.Input;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Services
{
    public interface IShortcutService
    {
        void HandleShortcut(KeyEventArgs e, bool isTextInputFocussed, bool isSettingsAudioFilesListViewFocussed);
    }

    public class ShortcutService(IEventHub eventHub) : IShortcutService
    {
        private readonly IEventHub _eventHub = eventHub;

        public void HandleShortcut(KeyEventArgs e, bool isTextInputFocussed, bool isSettingsAudioFilesListViewFocussed)
        {
            // We want these triggered regardless of focus
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Q)
            {
                _eventHub.Publish(new EditorAddRowShortcutActivatedEvent());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                _eventHub.Publish(new ViewerEditRowShortcutActivatedEvent());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                _eventHub.Publish(new EditorSetRecommendedVOSettingsShortcutActivatedEvent());
                e.Handled = true;
            }
            // We want to allow normal copy / paste / delete / backspace functionality when AudioProjectEditor text input is focussed
            else if (!isTextInputFocussed && !isSettingsAudioFilesListViewFocussed)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                {
                    _eventHub.Publish(new ViewerCopyRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
                {
                    _eventHub.Publish(new ViewerPasteRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
                else if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    _eventHub.Publish(new ViewerRemoveRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
            }
            // We want the Audio Files list in Settings to have its own delete / backspace functionality
            else if (isSettingsAudioFilesListViewFocussed)
            {
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    _eventHub.Publish(new SettingsRemoveSelectedAudioFilesShortcutActivatedEvent());
                    e.Handled = true;
                }
            }
        }
    }
}
