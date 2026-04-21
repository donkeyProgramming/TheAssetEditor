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
        List<Rmv2MeshNode> _takeAnimationFromList;

        public string HintText { get => "Skin wrap re-rigging"; }
        public bool IsMutation { get => true; }

        public SkinWrapRiggingCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(IEnumerable<Rmv2MeshNode> giveAnimationTo, List<Rmv2MeshNode> takeAnimationFrom)
        {
            _giveAnimationToList = giveAnimationTo.ToList();
            _takeAnimationFromList = takeAnimationFrom;
        }

        public void Execute()
        {
            _originalGeometries = _giveAnimationToList.Select(x => x.Geometry.Clone()).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            var firstSource = _takeAnimationFromList[0];
            foreach (var giveAnimationTo in _giveAnimationToList)
            {
                giveAnimationTo.Geometry.ChangeVertexType(firstSource.Geometry.VertexFormat, false);
                giveAnimationTo.Geometry.UpdateSkeletonName(firstSource.Geometry.SkeletonName);

                var maxBoneInfluences = giveAnimationTo.Geometry.WeightCount;

                for (var i = 0; i < giveAnimationTo.Geometry.VertexCount(); i++)
                {
                    var localVertexPos = giveAnimationTo.Geometry.VertexArray[i].Position3();
                    var worldVertexPos = localVertexPos + giveAnimationTo.Position;

                    var result = RegiggingHelper.FindClosestBoneWeightsMultiMesh(worldVertexPos, _takeAnimationFromList, maxBoneInfluences);

                    giveAnimationTo.Geometry.SetVertexBlendIndex(i, result.BoneIndices);
                    giveAnimationTo.Geometry.SetVertexWeights(i, result.BlendWeights);
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
