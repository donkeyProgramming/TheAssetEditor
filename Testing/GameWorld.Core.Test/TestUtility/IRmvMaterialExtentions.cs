using Shared.GameFormats.RigidModel;
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
            material.AlphaMode = useAlpha ? AlphaMode.Transparent : AlphaMode.Opaque;
            return material;
        }

        public static IRmvMaterial SetDecalAndDirt(this IRmvMaterial material, bool useDecal, bool useDirt)
        {
            if (material is WeightedMaterial weightedMaterial)
            {
                // Uv scale
                weightedMaterial.FloatParams.Add(2);
                weightedMaterial.FloatParams.Add(3);
                weightedMaterial.Vec4Params.Add(new RmvVector4(1, 2, 3, 4));
                weightedMaterial.IsDirtAndDecal = useDirt;
            }
            else
                throw new Exception("Not correct material type");

            return material;
        }
    }
}
