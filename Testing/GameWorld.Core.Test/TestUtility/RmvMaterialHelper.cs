using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace GameWorld.Core.Test.TestUtility
{
    public static class RmvMaterialHelper
    {
        public static IRmvMaterial Create(ModelMaterialEnum materialEnum)
        {
            var rmvMaterial = MaterialFactory.Create().CreateMaterial(materialEnum);
            return rmvMaterial;
        }
    }
}
