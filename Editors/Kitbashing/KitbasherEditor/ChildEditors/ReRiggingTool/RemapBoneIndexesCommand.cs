using CommunityToolkit.Diagnostics;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Shared.Ui.Editors.BoneMapping;
using Shared.Core.Events;


namespace Editors.KitbasherEditor.ChildEditors.ReRiggingTool
{
    public class RemapBoneIndexesCommand : IAeUndoCommandCommand
    {
        readonly SelectionManager _selectionManager;

        List <IndexRemapping> _mapping;
        string _newSkeletonName;
        List<Rmv2MeshNode> _meshNodeList;

        ISelectionState? _selectionOldState;
        List<MeshObject>? _originalGeometry;


        public RemapBoneIndexesCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<Rmv2MeshNode> meshNodeList, List<IndexRemapping> mapping, string newSkeletonName)
        {
            _meshNodeList = meshNodeList;
            _mapping = mapping;

            _newSkeletonName = newSkeletonName;
        }

        public string HintText { get => "Remap skeleton"; }
        public bool IsMutation { get => true; }

        public void Execute()
        {
            _selectionOldState = _selectionManager.GetStateCopy();

            _originalGeometry = _meshNodeList.Select(x => x.Geometry.Clone()).ToList();
            foreach (var geo in _originalGeometry)
                geo.RemoveGraphicsCardResources();
            foreach (var node in _meshNodeList)
            {
                node.Geometry.UpdateAnimationIndecies(_mapping);
                node.Geometry.UpdateSkeletonName(_newSkeletonName);
            }
        }

        public void Undo()
        {
            Guard.IsNotNull(_originalGeometry, $"{nameof(_originalGeometry)} Cannot undo before Execute is called");
            Guard.IsNotNull(_selectionOldState, $"{nameof(_selectionOldState)} Cannot undo before Execute is called");

            for (var i = 0; i < _meshNodeList.Count; i++)
            {
                _meshNodeList[i].Geometry.RemoveGraphicsCardResources();    // Remove all
                _originalGeometry[i].EnsureGraphicsResourcesCreated();      // Activte new
                _meshNodeList[i].Geometry = _originalGeometry[i];           // Swap
            }

            _selectionManager.SetState(_selectionOldState);
        }
    }
}
