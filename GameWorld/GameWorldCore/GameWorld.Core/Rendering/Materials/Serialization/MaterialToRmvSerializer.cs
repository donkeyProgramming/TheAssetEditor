using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToRmvSerializer
    {
        // The order CA's spec/gloss weighted models list their textures in, e.g.
        // weighted:          Diffuse, Normal, Specular, Mask, Gloss                                      (0,1,11,3,12)
        // weighted_dirtmap:  Diffuse, Normal, Decal_dirtmap, Decal_dirtmask, Specular, Mask, Gloss       (0,1,13,14,11,3,12)
        // decal_dirtmap:     ...the same, then Decal_mask                                                (0,1,13,14,11,3,12,15)
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

        // Stable sort: textures not in the order list keep their relative order, after the listed ones.
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
