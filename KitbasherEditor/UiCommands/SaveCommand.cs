using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SaveDialog;
using KitbasherEditor.Views.EditorViews;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly IWindowFactory _windowFactory;

        public SaveCommand(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create<SaveDialogViewModel, SaveDialogView>("Save", 600, 350);
            window.TypedContext.Initialise(window);
            window.ShowWindow();
        }
    }
}
