using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using System.Collections.Generic;
using System.Linq;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class DeleteLodsCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Delete all but first lod";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;


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
