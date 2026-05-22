namespace Editors.Shared.Core.Common
{
    public interface IMetaDataInstance
    {
        void CleanUp();
        void Update(float currentTime);
        GameWorld.Core.Animation.AnimationPlayer Player { get; }
    }
}
