using GameWorld.Core.Commands;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Shared.Ui.Editors.BoneMapping;
using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Commands.Object
{
    public class RemapBoneIndexesCommand : ICommand
    {
        List<IndexRemapping> _mapping;
        string _newSkeletonName;

        List<Rmv2MeshNode> _meshNodeList;
        List<MeshObject> _originalGeometry;
        string _originalSkeletonName;

        public void Configure(List<Rmv2MeshNode> meshNodeList, List<IndexRemapping> mapping, string newSkeletonName)
        {
            _meshNodeList = meshNodeList;
            _mapping = mapping;

            _newSkeletonName = newSkeletonName;
            _originalSkeletonName = _meshNodeList.First().Geometry.ParentSkeletonName;
        }

        public string HintText { get => "Remap skeleton"; }
        public bool IsMutation { get => true; }



        public void Execute()
        {
            _originalGeometry = _meshNodeList.Select(x => x.Geometry.Clone()).ToList();

            foreach (var node in _meshNodeList)
            {
                node.Geometry.UpdateAnimationIndecies(_mapping);
                node.Geometry.ParentSkeletonName = _newSkeletonName;
            }
        }

        public void Undo()
        {
            for (var i = 0; i < _meshNodeList.Count; i++)
            {
                _meshNodeList[i].Geometry = _originalGeometry[i];
                _meshNodeList[i].Geometry.ParentSkeletonName = _originalSkeletonName;
            }
        }
    }
}
