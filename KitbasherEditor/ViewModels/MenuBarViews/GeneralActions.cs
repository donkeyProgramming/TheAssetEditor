using CommonControls.Common;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GeneralActions : NotifyPropertyChangedImpl
    { 
        private readonly CommandExecutor _commandExecutor;
        private readonly FocusSelectableObjectService _cameraFocusComponent;       
        private readonly ObjectEditor _objectEditor;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneManager _sceneManager;
        private readonly SceneSaverService _sceneSaverService;
        private readonly WsModelGeneratorService _wsModelGeneratorService;

        public GeneralActions(CommandExecutor commandExecutor, FocusSelectableObjectService cameraFocusComponent,
            ObjectEditor objectEditor, RenderEngineComponent renderEngineComponent, SceneManager sceneManager, SceneSaverService sceneSaverService, WsModelGeneratorService wsModelGeneratorService)
        {
            _commandExecutor = commandExecutor;
            _cameraFocusComponent = cameraFocusComponent;
            _objectEditor = objectEditor;
            _renderEngineComponent = renderEngineComponent;
            _sceneManager = sceneManager;
            _sceneSaverService = sceneSaverService;
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        public void SortMeshes()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0 = rootNode.GetLodNodes().FirstOrDefault();
            if (lod0 != null)
                _objectEditor.SortMeshes(lod0);
        }

        public void DeleteLods()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lods = rootNode.GetLodNodes();

            var firtLod = lods.First();
            var lodsToGenerate = lods
                .Skip(1)
                .Take(rootNode.Children.Count - 1)
                .ToList();

            // Delete all the lods
            foreach (var lod in lodsToGenerate)
            {
                var itemsToDelete = new List<ISceneNode>();
                foreach (var child in lod.Children)
                    itemsToDelete.Add(child);

                foreach (var child in itemsToDelete)
                    child.Parent.RemoveObject(child);
            }
        }

        public void Save() => _sceneSaverService.Save();
        public void SaveAs() => _sceneSaverService.SaveAs();
        public void GenerateWsModelWh3()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _wsModelGeneratorService.GenerateWsModel(mainNode, CommonControls.Services.GameTypeEnum.Warhammer3);
        }

        public void Undo() => _commandExecutor.Undo();
        public void FocusSelection() => _cameraFocusComponent.FocusSelection();
        public void ResetCamera() => _cameraFocusComponent.ResetCamera();

        public void ToggleBackFaceRendering() => _renderEngineComponent.ToggelBackFaceRendering();
        public void ToggleLargeSceneRendering() => _renderEngineComponent.ToggleLargeSceneRendering();

        public void GenerateWsModelForWh2()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _wsModelGeneratorService.GenerateWsModel(mainNode, CommonControls.Services.GameTypeEnum.Warhammer2);
        }
    }
}
