using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using View3D.Animation;
using View3D.SceneNodes;

namespace Editors.Shared.Core.Services
{
    public interface IMetaDataInstance
    {
        void CleanUp();
        void Update(float currentTime);
        AnimationPlayer Player { get; }
    }

    public interface IMetaDataFactory
    {
        List<IMetaDataInstance> Create(MetaDataFile persistent, MetaDataFile metaData, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment);
    }

}
