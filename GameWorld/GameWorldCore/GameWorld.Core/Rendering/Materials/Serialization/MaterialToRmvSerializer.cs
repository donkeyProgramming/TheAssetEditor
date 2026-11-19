using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToRmvSerializer
    {
        // The order CA's spec/gloss weighted models list their textures in
        static readonly TextureType[] s_specGlossTextureOrder =
        [
            TextureType.Diffuse,
            TextureType.Normal,
            TextureType.Decal_dirtmap,
            TextureType.Decal_dirtmask,
            TextureType.Specular,
            TextureType.Mask,
            TextureType.Gloss,
            TextureType.Decal_mask,
        ];

        public IRmvMaterial CreateMaterialFromCapabilityMaterial(CapabilityMaterial material)
        {
            var newMaterial = MaterialFactory.Create().CreateMaterial(ModelMaterialEnum.weighted);

            foreach (var cap in material.Capabilities)
                cap.SerializeToRmvMaterial(newMaterial);

            if (material.TryGetCapability<SpecGlossCapability>() != null && newMaterial is WeightedMaterial weightedMaterial)
                SortTextures(weightedMaterial, s_specGlossTextureOrder);

            return newMaterial;
        }

        // Stable: unlisted textures keep their relative order, after the listed ones
        static void SortTextures(WeightedMaterial material, TextureType[] order)
        {
            int Rank(RmvTexture texture)
            {
                var index = Array.IndexOf(order, texture.TexureType);
                return index == -1 ? order.Length : index;
            }

            material.TexturesParams = material.TexturesParams.OrderBy(Rank).ToList();
        }
    }
}
