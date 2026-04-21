using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.ChildEditors.PinTool.Commands;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Services;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public partial class SkinWrapAlgorithm : ObservableObject
    {
        private readonly ILogger _logger = Logging.Create<SkinWrapAlgorithm>();
        private readonly IStandardDialogs _standardDialogs;
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        [ObservableProperty] ObservableCollection<Rmv2MeshNode> _sourceMeshes = [];

        public SkinWrapAlgorithm(CommandFactory commandFactory, IStandardDialogs standardDialogs, SelectionManager selectionManager)
        {
            _commandFactory = commandFactory;
            _standardDialogs = standardDialogs;
            _selectionManager = selectionManager;
        }

        [RelayCommand]
        void AddFromSelection()
        {
            var selectionState = _selectionManager.GetState<ObjectSelectionState>();
            if (selectionState == null || selectionState.SelectionCount() == 0)
            {
                _standardDialogs.ShowDialogBox("No mesh selected", "Error");
                return;
            }

            var selectedMeshes = selectionState.SelectedObjects()
                .OfType<Rmv2MeshNode>()
                .ToList();

            if (selectedMeshes.Count == 0)
            {
                _standardDialogs.ShowDialogBox("No mesh selected", "Error");
                return;
            }

            if (selectedMeshes.Any(x => x.PivotPoint != Vector3.Zero))
            {
                _standardDialogs.ShowDialogBox("Selected mesh has a pivot point, the tool will not work correctly", "error");
                return;
            }

            foreach (var mesh in selectedMeshes)
            {
                if (!SourceMeshes.Contains(mesh))
                    SourceMeshes.Add(mesh);
            }
        }

        [RelayCommand]
        void ClearSourceMeshes() => SourceMeshes.Clear();

        internal bool Execute(List<Rmv2MeshNode> giveAnimationTo)
        {
            if (SourceMeshes.Count == 0)
            {
                _standardDialogs.ShowDialogBox("No source meshes selected", "Error");
                return false;
            }

            var overlap = giveAnimationTo.Intersect(SourceMeshes).ToList();
            if (overlap.Count > 0)
            {
                _standardDialogs.ShowDialogBox("Same mesh found in both source and target lists", "error");
                return false;
            }

            _logger.Here().Information("Skin wrap: transferring rigging from {SourceCount} source meshes to {TargetCount} target meshes", SourceMeshes.Count, giveAnimationTo.Count);
            _commandFactory.Create<SkinWrapRiggingCommand>().Configure(x => x.Configure(giveAnimationTo, SourceMeshes.ToList())).BuildAndExecute();
            return true;
        }
    }
}
