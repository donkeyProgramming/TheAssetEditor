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
        CommandExecutor _commandExecutor;
        FocusSelectableObjectComponent _cameraFocusComponent;
        private readonly ComponentManagerResolver _componentManagerResolver;
        
        ObjectEditor _objectEditor;
        RenderEngineComponent _renderEngineComponent;
        private readonly SceneManager _sceneManager;

        public SceneSaverService ModelSaver { get; set; }
        public WsModelGeneratorService WsModelGeneratorService { get; set; }

        public GeneralActions(CommandExecutor commandExecutor, FocusSelectableObjectComponent cameraFocusComponent,
            ComponentManagerResolver componentManagerResolver, ObjectEditor objectEditor, RenderEngineComponent renderEngineComponent, SceneManager sceneManager )
        {
            _commandExecutor = commandExecutor;
            _cameraFocusComponent = cameraFocusComponent;
            _componentManagerResolver = componentManagerResolver;
            _objectEditor = objectEditor;
            _renderEngineComponent = renderEngineComponent;
            _sceneManager = sceneManager;
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

        public void Save() => ModelSaver.Save();
        public void SaveAs() => ModelSaver.SaveAs();
        public void GenerateWsModelWh3() => WsModelGeneratorService.GenerateWsModel(CommonControls.Services.GameTypeEnum.Warhammer3);
        public void Undo() => _commandExecutor.Undo();
        public void FocusSelection() => _cameraFocusComponent.FocusSelection();
        public void ResetCamera() => _cameraFocusComponent.ResetCamera();

        public void ToggleBackFaceRendering() => _renderEngineComponent.ToggelBackFaceRendering();
        public void ToggleLargeSceneRendering() => _renderEngineComponent.ToggleLargeSceneRendering();

        public void GenerateWsModelForWh2() => WsModelGeneratorService.GenerateWsModel(CommonControls.Services.GameTypeEnum.Warhammer2);
    }
}
