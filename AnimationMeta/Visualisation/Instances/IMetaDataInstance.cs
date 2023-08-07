using View3D.Animation;

namespace AnimationMeta.Visualisation.Instances
{
    public interface IMetaDataInstance
    {
        void CleanUp();
        void Update(float currentTime);
        AnimationPlayer Player { get; }
    }
}
