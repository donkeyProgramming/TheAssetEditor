using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
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
                weightedMaterial.UseDecal = useDecal;
                weightedMaterial.UseDirt = useDirt;
            }

            return material;
        }
    }
}
