using Common;
using CommonControls.Common;
using CommonControls.PackFileBrowser;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Services;
using static View3D.Commands.Object.GroupObjectsCommand;

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

        public void Save() => ModelSaver.Save();
        public void SaveAs() => ModelSaver.SaveAs();
        public void GenerateWsModel() => ModelSaver.GenerateWsModel();
        public void Undo() => _commandExecutor.Undo();
        public void FocusSelection() => _cameraFocusComponent.FocusSelection();
        public void ResetCamera() => _cameraFocusComponent.ResetCamera();

        public void ToggleBackFaceRendering() => _renderEngineComponent.ToggelBackFaceRendering();
        public void ToggleLargeSceneRendering() => _renderEngineComponent.ToggleLargeSceneRendering();
    }
}
