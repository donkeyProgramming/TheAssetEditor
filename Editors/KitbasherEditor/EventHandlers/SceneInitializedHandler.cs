using System.Windows;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.WpfWindow.Events;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace KitbasherEditor.EventHandlers
{
    public class SceneInitializedHandler
    {
        private readonly ILogger _logger = Logging.Create<SceneInitializedHandler>();

        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly PackFileService _packFileService;
        private readonly EventHub _eventHub;
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly SaveSettings _saveSettings;
        private readonly SceneManager _sceneManager;
        private readonly FocusSelectableObjectService _focusSelectableObjectComponent;

        public SceneInitializedHandler(
            KitbasherViewModel kitbasherViewModel,
            PackFileService packFileService,
            EventHub eventHub,
            KitbashSceneCreator kitbashSceneCreator,
            SaveSettings saveSettings,
            SceneManager sceneManager,
            FocusSelectableObjectService focusSelectableObjectComponent)
        {
            _kitbasherViewModel = kitbasherViewModel;
            _packFileService = packFileService;
            _eventHub = eventHub;
            _kitbashSceneCreator = kitbashSceneCreator;
            _saveSettings = saveSettings;
            _sceneManager = sceneManager;
            _focusSelectableObjectComponent = focusSelectableObjectComponent;

            _eventHub.Register<SceneInitializedEvent>(OnSceneInitialized);
        }

        void OnSceneInitialized(SceneInitializedEvent notification)
        {
            var fileToLoad = _kitbasherViewModel.MainFile;
            try
            {
                var fileName = _packFileService.GetFullPath(fileToLoad);
                _kitbashSceneCreator.Create();
                _kitbashSceneCreator.LoadMainEditableModel(fileToLoad);

                _focusSelectableObjectComponent.FocusScene();
                ConfigureDefaultSettings(fileName);

                _kitbasherViewModel.DisplayName.Value = fileToLoad.Name;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading file {fileToLoad?.Name} - {e}");
                MessageBox.Show($"Unable to load file\n+{e.Message}");
            }

            _eventHub.UnRegister<SceneInitializedEvent>(OnSceneInitialized);
        }

        void ConfigureDefaultSettings(string fileName)
        {
            _saveSettings.OutputName = fileName;
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _saveSettings.InitializeFromModel(mainNode);
        }

        public static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            if(float.IsNaN(lerpValue))
                lerpValue = 1;
            var deductionRatio = MathHelper.Lerp(0.25f, 1, lerpValue);
            return deductionRatio;
        }
    }
}
