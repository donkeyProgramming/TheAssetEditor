using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.ChildEditors.PhotoStudio
{
    internal class OpenPhotoStudioCommand : IScopedKitbasherUiCommand
    {
        private readonly IAbstractFormFactory<PhotoStudioWindow> _windowFactory;
        PhotoStudioWindow? _windowInstance;

        public string ToolTip { get; set; } = "Open PhotStudio";

        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;

        public Hotkey? HotKey => new(Key.P, ModifierKeys.Control);

        public OpenPhotoStudioCommand(IAbstractFormFactory<PhotoStudioWindow> windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            if (_windowInstance == null)
            {
                _windowInstance = _windowFactory.Create();
                _windowInstance.Show();
                _windowInstance.Closed += OnWindowClosed;
            }
            else
            {
                _windowInstance.BringIntoView();
            }
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_windowInstance != null)
                _windowInstance.Closed -= OnWindowClosed;

            _windowInstance = null;
        }

        public void Dispose()
        {
            if (_windowInstance != null)
                _windowInstance.Close();
            _windowInstance = null;
        }
    }
}
