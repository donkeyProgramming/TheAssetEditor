using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.ChildEditors.PinTool.Commands
{
    public class SkinWrapRiggingCommand : ICommand
    {
        ISelectionState _selectionOldState;
        private readonly SelectionManager _selectionManager;

        List<MeshObject> _originalGeometries;

        List<Rmv2MeshNode> _giveAnimationToList;
        Rmv2MeshNode _takeAnimationFrom;

        public string HintText { get => "Skin wrap re-rigging"; }
        public bool IsMutation { get => true; }



        public SkinWrapRiggingCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager; ;
        }

        public void Configure(IEnumerable<Rmv2MeshNode> giveAnimationTo, Rmv2MeshNode takeAnimationFrom)
        {
            _giveAnimationToList = giveAnimationTo.ToList();
            _takeAnimationFrom = takeAnimationFrom;
        }

        public void Execute()
        {
            // Create undo state
            _originalGeometries = _giveAnimationToList.Select(x => x.Geometry.Clone()).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            // Update the meshes
            foreach (var giveAnimationTo in _giveAnimationToList)
            {
                // Set skeleton and vertex type from first source object
                giveAnimationTo.Geometry.ChangeVertexType(_takeAnimationFrom.Geometry.VertexFormat, false);
                giveAnimationTo.Geometry.UpdateSkeletonName(_takeAnimationFrom.Geometry.SkeletonName);

                for (var i = 0; i < giveAnimationTo.Geometry.VertexCount(); i++)
                {
                    var inputVertexPos = giveAnimationTo.Geometry.VertexArray[i].Position3();
                    var res = RegiggingHelper.FindClosestUV(inputVertexPos, _takeAnimationFrom.Geometry, _takeAnimationFrom.Position);

                    giveAnimationTo.Geometry.VertexArray[i].BlendIndices = res.Bones;
                    giveAnimationTo.Geometry.VertexArray[i].BlendWeights = res.BlendWeights;
                }

                giveAnimationTo.Geometry.RebuildVertexBuffer();
            }
        }

        public void Undo()
        {
            for (var i = 0; i < _giveAnimationToList.Count; i++)
                _giveAnimationToList[i].Geometry = _originalGeometries[i];

            _selectionManager.SetState(_selectionOldState);
        }
    }
}
