using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using System;

namespace Editors.AnimationMeta.Visualisation.Instances
{
    public class DrawableMetaInstance : IMetaDataInstance
    {
        private readonly ILogger _logger = Logging.Create<MetaDataFactory>();
        private bool _hasError = false;

        private readonly SceneNode _node;
        private readonly string _description;
        public AnimationPlayer Player => null;

        private SkeletonBoneAnimationResolver _animationResolver;

        public DrawableMetaInstance(float startTime, float endTime, string description, SceneNode node)
        {
            _description = description;
            _node = node;
        }

        public void FollowBone(ISkeletonProvider skeleton, int boneIndex)
        {
            if (boneIndex != -1)
                _animationResolver = new SkeletonBoneAnimationResolver(skeleton, boneIndex);
        }

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
