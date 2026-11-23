using Editors.AnimationMeta.SuperView.Editing;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Visualisation.Instances
{
    // A PROP or ANIMATED_PROP tag's model on its bone. A prop with its own animation is placed by the CopyRootTransform
    // rule on its player; one without is placed here, on every update. Either way the place is read from the tag, so the
    // model follows a drag in the viewport before it is rebuilt.
    public class AnimatedPropInstance : IMetaDataInstance, IMetaDataMarkerPreview
    {
        private readonly SceneNode _node;
        private readonly SpatialMetaDataBinding? _binding;
        private readonly ISkeletonProvider? _skeleton;
        private readonly bool _placedByAnimation;

        public AnimationPlayer Player { get; private set; }
        public ParsedMetadataAttribute Source => _binding?.Source!;

        public AnimatedPropInstance(SceneNode node, AnimationPlayer player, SpatialMetaDataBinding? binding = null, ISkeletonProvider? skeleton = null, bool placedByAnimation = true)
        {
            _node = node;
            Player = player;
            _binding = binding;
            _skeleton = skeleton;
            _placedByAnimation = placedByAnimation;
        }

        // The tag's position and orientation, in its bone's frame.
        Matrix Local => _binding == null ? Matrix.Identity : Matrix.CreateFromQuaternion(_binding.Orientation ?? Quaternion.Identity) * Matrix.CreateTranslation(_binding.Position);

        Matrix Bone
        {
            get
            {
                var skeleton = _skeleton?.Skeleton;
                var bone = _binding?.BoneIndex ?? -1;
                return skeleton != null && bone >= 0 && bone < skeleton.BoneCount ? skeleton.GetAnimatedWorldTranform(bone) : Matrix.Identity;
            }
        }

        public bool IsHitTestVisible => _binding != null && _node.IsVisible && _node.Parent != null;
        public float HitTestRadius => 0.3f;
        public bool FollowsTagLive => true;
        public Matrix ReferenceWorldTransform => Bone * MetaDataPreviewMath.WorldOf(_node.Parent);
        public Vector3 FocusPosition => (Local * ReferenceWorldTransform).Translation;
        public IReadOnlyList<MetaDataMarkerHitTarget> HitTargets => _binding == null ? [] : [new(FocusPosition, MetaDataMarkerPoint.Default)];

        public void Update(float currentTime)
        {
            if (_binding != null && !_placedByAnimation)
                _node.ModelMatrix = Local * Bone;
        }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
            Player.MarkedForRemoval = true;
        }
    }
}
