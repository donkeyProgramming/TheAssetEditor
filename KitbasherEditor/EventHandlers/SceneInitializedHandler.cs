using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Services.SceneSaving;
using View3D.Services.SceneSaving.Geometry;

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
        private readonly SaveSettings _saveSettings;
        private readonly SceneManager _sceneManager;
        private readonly FocusSelectableObjectService _focusSelectableObjectComponent;

        public SceneInitializedHandler(
            KitbasherViewModel kitbasherViewModel,
            PackFileService packFileService,
            KitbasherRootScene kitbasherRootScene,
            EventHub eventHub,
            KitbashSceneCreator kitbashSceneCreator,
            SaveSettings saveSettings,
            SceneManager sceneManager,
            FocusSelectableObjectService focusSelectableObjectComponent)
        {
            _kitbasherViewModel = kitbasherViewModel;
            _packFileService = packFileService;
            _kitbasherRootScene = kitbasherRootScene;
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
            _saveSettings.GeometryOutputType = GeometryStrategy.Rmv7;  //   _kitbasherRootScene.SelectedOutputFormat = rmv.Header.Version;

            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var numLods = mainNode.Model.LodHeaders.Count();
            var lodValues = new List<LodGenerationSettings>();

            for(int i = 0; i <  numLods; i++) 
            {
                var lodHeader = mainNode.Model.LodHeaders[i];

                var setting = new LodGenerationSettings()
                {
                    CameraDistance = lodHeader.LodCameraDistance,
                    QualityLvl = lodHeader.QualityLvl,
                    LodRectionFactor = GetDefaultLodReductionValue(numLods, i),
                    OptimizeAlpha = i >= 2 ? true : false,
                    OptimizeVertex = i >= 2 ? true : false,
                };
                lodValues.Add(setting);
            }

            _saveSettings.LodSettingsPerLod = lodValues.ToArray();
        }

        public static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            var deductionRatio = MathHelper.Lerp(0.25f, 1, lerpValue);
            return deductionRatio;
        }
    }
}
