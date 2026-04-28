namespace GameWorld.Core.Animation.AnimationChange
{
    public interface IAnimationChangeRule
    {
    }

    public interface IWorldSpaceAnimationRule : IAnimationChangeRule
    {
        void TransformFrameWorldSpace(AnimationFrame frame, float time);
    }

    public interface ILocalSpaceAnimationRule : IAnimationChangeRule
    {
        void TransformFrameLocalSpace(AnimationFrame frame, int boneId, float time);
    }


}
