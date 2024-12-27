using Editors.Shared.Core.Common;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.AnimatioReTarget.Editor.BoneHandling
{
    class SkeletonBoneHighlighter : ISkeletonBoneHighlighter
    {
        private readonly SceneObject _source;
        private readonly SceneObject _target;
        public SkeletonBoneHighlighter(SceneObject source, SceneObject target)
        {
            _source = source;
            _target = target;
        }

        public void SelectSourceSkeletonBone(int index) => _source.SelectedBoneIndex(index);
        public void SelectTargetSkeletonBone(int index) => _target.SelectedBoneIndex(index);
    }
}

