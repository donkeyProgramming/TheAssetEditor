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

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GeneralActions : NotifyPropertyChangedImpl
    { 
        CommandExecutor _commandExecutor;
        FocusSelectableObjectComponent _cameraFocusComponent;
        IEditableMeshResolver _editableMeshResolver;
        ObjectEditor _objectEditor;
        RenderEngineComponent _renderEngineComponent;

        public SceneSaverService ModelSaver { get; set; }
        public WsModelGeneratorService WsModelGeneratorService { get; set; }

        public GeneralActions(IComponentManager componentManager)
        {
            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _cameraFocusComponent = componentManager.GetComponent<FocusSelectableObjectComponent>();
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
            _objectEditor = componentManager.GetComponent<ObjectEditor>();
            _renderEngineComponent = componentManager.GetComponent<RenderEngineComponent>();
        }

        public void SortMeshes()
        {
            var lod0 = _editableMeshResolver.GeEditableMeshRootNode().GetLodNodes().FirstOrDefault();
            if (lod0 != null)
                _objectEditor.SortMeshes(lod0);
        }

        public void DeleteLods()
        {
            var rootNode = _editableMeshResolver.GeEditableMeshRootNode();
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
