namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public static class RmvMaterialUtil
    {
        public static bool IsDecal(IRmvMaterial rmvMaterial)
        {
            ModelMaterialEnum[] types = [
                ModelMaterialEnum.decal,            ModelMaterialEnum.weighted_decal,           ModelMaterialEnum.weighted_skin_decal_dirtmap,
                ModelMaterialEnum.decal_dirtmap,    ModelMaterialEnum.weighted_decal_dirtmap,   ModelMaterialEnum.weighted_skin_decal, ];

            if (types.Contains(rmvMaterial.MaterialId))
                return true;
            return false;
        }

        public static bool IsDirt(IRmvMaterial rmvMaterial)
        {
            ModelMaterialEnum[] types = [
                ModelMaterialEnum.dirtmap,          ModelMaterialEnum.weighted_dirtmap,
                ModelMaterialEnum.decal_dirtmap,    ModelMaterialEnum.weighted_decal_dirtmap, ModelMaterialEnum.weighted_skin_dirtmap];

            if (types.Contains(rmvMaterial.MaterialId))
                return true;
            return false;
        }

        public static bool IsSkin(IRmvMaterial rmvMaterial)
        {
            ModelMaterialEnum[] types = [
                ModelMaterialEnum.weighted_skin,          ModelMaterialEnum.weighted_skin_decal_dirtmap,
                ModelMaterialEnum.weighted_skin_decal,    ModelMaterialEnum.weighted_skin_dirtmap];

            if (types.Contains(rmvMaterial.MaterialId))
                return true;
            return false;
        }

        public static WeightedMaterial.MaterialHintEnum GetMaterialHint(bool useDirt, bool useDecal, bool useSkin)
        {
            if (useSkin)
            {
                if (useDirt)
                    return WeightedMaterial.MaterialHintEnum.Skin_Dirt;
                return WeightedMaterial.MaterialHintEnum.Skin;
            }

            if (useDecal)
            {
                if(useDirt)
                    return WeightedMaterial.MaterialHintEnum.Decal_Dirt;

                return WeightedMaterial.MaterialHintEnum.Decal;
            }

            if (useDirt)
            {
                if (useSkin)
                    return WeightedMaterial.MaterialHintEnum.Skin_Dirt;
                if (useDecal)
                    return WeightedMaterial.MaterialHintEnum.Decal_Dirt;

                return WeightedMaterial.MaterialHintEnum.Dirt;
            }


            throw new Exception($"Unknown material combo useDirt:{useDirt} useDecal:{useDecal} useSkin:{useSkin}");
        }
    }
}
