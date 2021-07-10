using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    public class RemapBoneIndexesCommand : CommandBase<RemapBoneIndexesCommand>
    {
        List<IndexRemapping> _mapping;
        string _newSkeletonName;

        Rmv2MeshNode _meshOwner;
        IGeometry _originalGeometry;
        string _originalSkeletonName;

        GameSkeleton _targetSkeleton;
        GameSkeleton _currentSkeleton;

        public RemapBoneIndexesCommand(Rmv2MeshNode meshOwner, List<IndexRemapping> mapping, string newSkeletonName, GameSkeleton currentSkeleton, GameSkeleton targetSkeleton)
        {
            _meshOwner = meshOwner;
            _mapping = mapping;
            _newSkeletonName = newSkeletonName;
            _currentSkeleton = currentSkeleton;
            _targetSkeleton = targetSkeleton;
        }


        public override string GetHintText()
        {
            return "Remap skeleton";
        }

        protected override void ExecuteCommand()
        {
            _originalGeometry = _meshOwner.Geometry.Clone();
            _originalSkeletonName = _meshOwner.MeshModel.ParentSkeletonName;
            _meshOwner.Geometry.UpdateAnimationIndecies(_mapping);
            _meshOwner.MeshModel.ParentSkeletonName = _newSkeletonName;
        }

        protected override void UndoCommand()
        {
            _meshOwner.Geometry = _originalGeometry;
            _meshOwner.MeshModel.ParentSkeletonName = _originalSkeletonName;
        }
    }
}
