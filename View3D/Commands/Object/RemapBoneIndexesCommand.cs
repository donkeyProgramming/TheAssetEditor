using CommonControls.Editors.BoneMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class RemapBoneIndexesCommand : CommandBase<RemapBoneIndexesCommand>
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

        public override string GetHintText()
        {
            return "Remap skeleton";
        }

        protected override void ExecuteCommand()
        {
            _originalGeometry = _meshNodeList.Select(x => x.Geometry.Clone()).ToList();

            foreach (var node in _meshNodeList)
            {
                node.Geometry.UpdateAnimationIndecies(_mapping);
                node.Geometry.ParentSkeletonName = _newSkeletonName;
            }
        }

        protected override void UndoCommand()
        {
            for (int i = 0; i < _meshNodeList.Count; i++)
            {
                _meshNodeList[i].Geometry = _originalGeometry[i];
                _meshNodeList[i].Geometry.ParentSkeletonName = _originalSkeletonName;
            }
        }
    }
}
