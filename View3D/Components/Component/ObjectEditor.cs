using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Rendering;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class ObjectEditor : BaseComponent
    {
        ILogger _logger = Logging.Create<ObjectEditor>();

        CommandExecutor _commandManager;

        public ObjectEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _commandManager = GetComponent<CommandExecutor>();
            base.Initialize();
        }


        public void DeleteObject(ObjectSelectionState objectSelectionState)
        {
            var selection = objectSelectionState.CurrentSelection();
            if (selection.Count != 0)
            {
                var command = new DeleteObjectsCommand(selection);
                _commandManager.ExecuteCommand(command);
            }
        }

        public void DuplicateObject(ObjectSelectionState objectSelectionState)
        {
            if (objectSelectionState.CurrentSelection().Count != 0)
            {
                var command = new DuplicateObjectCommand(objectSelectionState.CurrentSelection().Select(x => (ISceneNode)x).ToList());
                _commandManager.ExecuteCommand(command);
            }
        }

        public void DivideIntoSubmeshes(ObjectSelectionState objectSelectionState)
        {
            if (objectSelectionState.GetSingleSelectedObject() is IEditableGeometry drawableNode)
            {
                var command = new DivideObjectIntoSubmeshesCommand(drawableNode);
                _commandManager.ExecuteCommand(command);
            }
        }

        public bool CombineMeshes(ObjectSelectionState objectSelectionState, out List<string> errorMessages)
        {
            ModelCombiner modelValidator = new ModelCombiner();
            var objs = objectSelectionState.SelectedObjects().Where(x => x is Rmv2MeshNode).Select(x => x as Rmv2MeshNode);
            if (!modelValidator.CanCombine(objs.ToList(), out errorMessages))
                return false;

            var command = new CombineMeshCommand(objectSelectionState.SelectedObjects());
            _commandManager.ExecuteCommand(command);
            
            return true;
        }

        public void ReduceSelection(List<ISelectable> selectable, float factor)
        {
            throw new NotImplementedException();
        }
    }
}
