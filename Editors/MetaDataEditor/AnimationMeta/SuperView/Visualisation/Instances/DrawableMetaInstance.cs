using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Visualisation.Instances
{
    // A tag drawn as lines and labels: IMPACT_POS, TARGET_POS, FIRE_POS or SPLASH_ATTACK. Its points are read from the
    // tag each time they are asked for, so they follow an edit before the drawing is rebuilt.
    public class DrawableMetaInstance : IMetaDataInstance, IMetaDataMarkerPreview
    {
        private readonly ILogger _logger = Logging.Create<MetaDataBuilder>();
        private bool _hasError = false;

        private readonly SceneNode _node;
        private readonly Func<IReadOnlyList<MetaDataMarkerHitTarget>> _localPoints;
        public AnimationPlayer Player => null;
        public ParsedMetadataAttribute Source { get; }

        private SkeletonBoneAnimationResolver? _animationResolver;

        public DrawableMetaInstance(ParsedMetadataAttribute source, SceneNode node, Func<IReadOnlyList<MetaDataMarkerHitTarget>> localPoints)
        {
            Source = source;
            _node = node;
            _localPoints = localPoints;
        }

        public void FollowBone(ISkeletonProvider skeleton, int boneIndex)
        {
            if (boneIndex != -1)
                _animationResolver = new SkeletonBoneAnimationResolver(skeleton, boneIndex);
        }

        public bool IsHitTestVisible => _node.IsVisible && _node.Parent != null;
        public float HitTestRadius => 0.3f;

        // The bone it follows, if any, inside the node it is drawn under.
        public Matrix ReferenceWorldTransform => (_animationResolver?.GetWorldTransform() ?? Matrix.Identity) * MetaDataPreviewMath.WorldOf(_node.Parent);

        public IReadOnlyList<MetaDataMarkerHitTarget> HitTargets
        {
            get
            {
                var reference = ReferenceWorldTransform;
                return _localPoints().Select(x => x with { Position = Vector3.Transform(x.Position, reference) }).ToArray();
            }
        }

        public Vector3 FocusPosition => HitTargets.FirstOrDefault().Position;

        public void Update(float currentTime)
        {
            if (_hasError)
                return;

            try
            {
                if (_animationResolver != null)
                    _node.ModelMatrix = _animationResolver.GetWorldTransform();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error in {nameof(DrawableMetaInstance)} - {e.Message}");
                _hasError = true;
            }
        }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
        }
    }
}
