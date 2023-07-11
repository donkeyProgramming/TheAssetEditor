using CommonControls.Events.UiCommands;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class DeleteLodsCommand : IExecutableUiCommand
    {
        SceneManager _sceneManager;

        public DeleteLodsCommand(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lods = rootNode.GetLodNodes();

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
    }
}
