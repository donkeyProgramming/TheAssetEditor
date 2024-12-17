using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.ChildEditors.PinTool.Commands;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Services;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public partial class SkinWrapAlgorithm : ObservableObject
    {
        private readonly IStandardDialogs _standardDialogs;
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        [ObservableProperty] Rmv2MeshNode _takeAnimationFromMesh;
        [ObservableProperty] string _description = "";

        public SkinWrapAlgorithm(CommandFactory commandFactory, IStandardDialogs standardDialogs, SelectionManager selectionManager)
        {
            _commandFactory = commandFactory;
            _standardDialogs = standardDialogs;
            _selectionManager = selectionManager;
        }

        [RelayCommand] void SetSelection()
        {
            TakeAnimationFromMesh = null;

            var description = "No Mesh selected";
            var selectionState = _selectionManager.GetState<ObjectSelectionState>();
            if (selectionState == null || selectionState.SelectionCount() == 0)
            {
                _standardDialogs.ShowDialogBox("No mesh selected", "Error");
                return;
            }

            if(selectionState.SelectionCount() != 1)
            {
                _standardDialogs.ShowDialogBox("Multiple meshes selected - pick one", "Error");
                return;
            }

            var selectionAsMeshNode = selectionState.GetSingleSelectedObject() as Rmv2MeshNode;
            if (selectionAsMeshNode == null)
                throw new Exception($"Unexpected result for selection. State = {selectionState}");

            if (selectionAsMeshNode.PivotPoint != Vector3.Zero)
            {
                _standardDialogs.ShowDialogBox("Selected mesh has a pivot point, the tool will not work correctly", "error");
                return;
            }

            TakeAnimationFromMesh = selectionAsMeshNode;

            description = $"{TakeAnimationFromMesh.Name}'";
            Description = description;
        }


        internal bool Excute(List<Rmv2MeshNode> giveAnimationTo)
        {
            var isAlsoInInputList = giveAnimationTo.Contains(TakeAnimationFromMesh);
            if (isAlsoInInputList)
            {
                _standardDialogs.ShowDialogBox("Same mesh found in both lists", "error");
                return false;
            }

            _commandFactory.Create<SkinWrapRiggingCommand>().Configure(x => x.Configure(giveAnimationTo, TakeAnimationFromMesh)).BuildAndExecute();
            return true;
        }
    }
}
