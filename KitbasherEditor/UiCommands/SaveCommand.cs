using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SaveDialog;
using KitbasherEditor.Views.EditorViews;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services.SceneSaving.Geometry;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly SceneSaverService _sceneSaverService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly IWindowFactory _windowFactory;
        private readonly SceneManager _sceneManager;

        public SaveCommand(SceneSaverService sceneSaverService, KitbasherRootScene kitbasherRootScene, IWindowFactory windowFactory, SceneManager sceneManager)
        {
            _sceneSaverService = sceneSaverService;
            _kitbasherRootScene = kitbasherRootScene;
            _windowFactory = windowFactory;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            var window = _windowFactory.Create<SaveDialogViewModel, SaveDialogView>("Save", 600, 350);
            window.TypedContext.Initialise(window);
            window.ShowWindow();
        }
    }
}
