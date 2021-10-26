using AnimationEditor.Common.ReferenceModel;
using CommonControls.Editors.BoneMapping;

namespace AnimationEditor.AnimationTransferTool
{
    class SkeletonBoneHighlighter : ISkeletonBoneHighlighter
    {
        AssetViewModel _source;
        AssetViewModel _target;
        public SkeletonBoneHighlighter(AssetViewModel source, AssetViewModel target)
        {
            _source = source;
            _target = target;
        }

        public void SelectSourceSkeletonBone(int index)
        {
            _source.SelectedBoneIndex( index);
        }

        public void SelectTargetSkeletonBone(int index)
        {
            _target.SelectedBoneIndex(index);
        }
    }
}

