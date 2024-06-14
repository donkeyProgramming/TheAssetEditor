using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;

namespace Editors.AnimationMeta.Visualisation.Instances
{
    public class AnimatedPropInstance : IMetaDataInstance
    {
        private readonly SceneNode _node;

        public AnimationPlayer Player { get; private set; }

        public AnimatedPropInstance(SceneNode node, AnimationPlayer player)
        {
            _node = node;
            Player = player;
        }

        public void Update(float currentTime)
        { }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
            Player.MarkedForRemoval = true;
        }
    }
}
