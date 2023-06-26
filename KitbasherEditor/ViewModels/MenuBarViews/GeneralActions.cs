using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GeneralActions : NotifyPropertyChangedImpl
    { 
        private readonly CommandExecutor _commandExecutor;
        private readonly FocusSelectableObjectComponent _cameraFocusComponent;       
        private readonly ObjectEditor _objectEditor;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneManager _sceneManager;
        private readonly SceneSaverService _sceneSaverService;
        public WsModelGeneratorService WsModelGeneratorService { get; set; }

        public GeneralActions(CommandExecutor commandExecutor, FocusSelectableObjectComponent cameraFocusComponent,
            ObjectEditor objectEditor, RenderEngineComponent renderEngineComponent, SceneManager sceneManager, SceneSaverService sceneSaverService )
        {
            _commandExecutor = commandExecutor;
            _cameraFocusComponent = cameraFocusComponent;
            _objectEditor = objectEditor;
            _renderEngineComponent = renderEngineComponent;
            _sceneManager = sceneManager;
            _sceneSaverService = sceneSaverService;
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
        public void GenerateWsModelWh3() => WsModelGeneratorService.GenerateWsModel(CommonControls.Services.GameTypeEnum.Warhammer3);
        public void Undo() => _commandExecutor.Undo();
        public void FocusSelection() => _cameraFocusComponent.FocusSelection();
        public void ResetCamera() => _cameraFocusComponent.ResetCamera();

        public void ToggleBackFaceRendering() => _renderEngineComponent.ToggelBackFaceRendering();
        public void ToggleLargeSceneRendering() => _renderEngineComponent.ToggleLargeSceneRendering();

        public void GenerateWsModelForWh2() => WsModelGeneratorService.GenerateWsModel(CommonControls.Services.GameTypeEnum.Warhammer2);
    }
}
