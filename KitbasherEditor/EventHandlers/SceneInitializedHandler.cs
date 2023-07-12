using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Windows;
using View3D.Services;

namespace KitbasherEditor.EventHandlers
{
    public class SceneInitializedHandler
    {
        ILogger _logger = Logging.Create<SceneInitializedHandler>();

        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly PackFileService _packFileService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly EventHub _eventHub;
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly FocusSelectableObjectService _focusSelectableObjectComponent;

        public SceneInitializedHandler(
            KitbasherViewModel kitbasherViewModel,
            PackFileService packFileService,
            KitbasherRootScene kitbasherRootScene,
            EventHub eventHub,
            KitbashSceneCreator kitbashSceneCreator,
            FocusSelectableObjectService focusSelectableObjectComponent)
        {
            _kitbasherViewModel = kitbasherViewModel;
            _packFileService = packFileService;
            _kitbasherRootScene = kitbasherRootScene;
            _eventHub = eventHub;
            _kitbashSceneCreator = kitbashSceneCreator;
            _focusSelectableObjectComponent = focusSelectableObjectComponent;

            _eventHub.Register<SceneInitializedEvent>(OnSceneInitialized);
        }

        void OnSceneInitialized(SceneInitializedEvent notification)
        {
            var fileToLoad = _kitbasherViewModel.MainFile;
            try
            {
                _kitbasherRootScene.ActiveFileName = _packFileService.GetFullPath(fileToLoad);

                _kitbashSceneCreator.Create();
                _kitbashSceneCreator.LoadMainEditableModel(fileToLoad);

                _focusSelectableObjectComponent.FocusScene();

                _kitbasherViewModel.DisplayName.Value = fileToLoad.Name;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading file {fileToLoad?.Name} - {e}");
                MessageBox.Show($"Unable to load file\n+{e.Message}");
            }

            _eventHub.UnRegister<SceneInitializedEvent>(OnSceneInitialized);
        }
    }
}
