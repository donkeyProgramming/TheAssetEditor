using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Test.TestUtility
{
    public static class IRmvMaterialExtentions
    {
        public static IRmvMaterial AssignMaterials(this IRmvMaterial material, TextureType[] texturesToAssign)
        {
            foreach (var texture in texturesToAssign)
                material.SetTexture(texture, $"texturePath/{texture}.dds");

            return material;
        }

        public static IRmvMaterial SetAlpha(this IRmvMaterial material, bool useAlpha)
        {

            (material as WeightedMaterial).IntParams.Set(WeightedParamterIds.IntParams_Alpha_index, useAlpha ? 1 : 0);
            return material;
        }

        public static IRmvMaterial SetDecalAndDirt(this IRmvMaterial material, bool useDecal, bool useDirt)
        {
            if (material is WeightedMaterial weightedMaterial)
            {
                // Uv scale
                weightedMaterial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleX, 2);
                weightedMaterial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleX, 3);
                weightedMaterial.Vec4Params.Set(WeightedParamterIds.Vec4Params_TextureDecalTransform, new RmvVector4(1, 2, 3, 4));
                weightedMaterial.IntParams.Set(WeightedParamterIds.IntParams_Dirt_index, useDirt ? 1: 0);
                weightedMaterial.IntParams.Set(WeightedParamterIds.IntParams_Decal_index, useDecal ? 1 : 0);
            }
            else
                throw new Exception("Not correct material type");

            return material;
        }
    }
}
