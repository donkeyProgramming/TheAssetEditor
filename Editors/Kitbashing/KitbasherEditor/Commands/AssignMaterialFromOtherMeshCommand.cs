using CommunityToolkit.Diagnostics;
using GameWorld.Core.Commands;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;


namespace Editors.KitbasherEditor.Commands
{
    internal class AssignMaterialFromOtherMeshCommand : IAeUndoCommandCommand
    {
        record MeshMaterialHistory(Rmv2MeshNode Mesh, CapabilityMaterial Material);

        readonly ILogger _logger;
        public string HintText => "Assign Material";
        public bool IsMutation => true;

        
        readonly List<MeshMaterialHistory> _history = [];
        CapabilityMaterial? _newMaterial;

        public AssignMaterialFromOtherMeshCommand(IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<AssignMaterialFromOtherMeshCommand>();
        }

        public void Execute()
        {
            Guard.IsNotNull(_newMaterial, "New material cannot be null when executing AssignMaterialFromOtherMeshCommand"); 

            foreach (var historyItem in _history)
                historyItem.Mesh.Material = _newMaterial.Clone();
        }

        public void Configure(CapabilityMaterial materialToAssign, List<Rmv2MeshNode> meshesToAssignTo)
        {
            _logger.Here().Information("Assigning material {MaterialName} to {MeshCount} meshes", meshesToAssignTo.Count, materialToAssign.Type.ToString());

            _newMaterial = materialToAssign;
            foreach (var mesh in meshesToAssignTo)
            { 
                var historyItem = new MeshMaterialHistory(mesh, mesh.Material);
                _history.Add(historyItem);
            }
        }

        public void Undo()
        {
            foreach (var historyItem in _history)
                historyItem.Mesh.Material = historyItem.Material;
        }
    }
}
