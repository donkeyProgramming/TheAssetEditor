namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public static class RmvMaterialUtil
    {
        public static bool IsDecal(IRmvMaterial rmvMaterial)
        {
            ModelMaterialEnum[] types = [
                ModelMaterialEnum.decal,            ModelMaterialEnum.weighted_decal, 
                ModelMaterialEnum.decal_dirtmap,    ModelMaterialEnum.weighted_decal_dirtmap];

            if (types.Contains(rmvMaterial.MaterialId))
                return true;
            return false;
        }

        public static bool IsDirt(IRmvMaterial rmvMaterial)
        {
            ModelMaterialEnum[] types = [
                ModelMaterialEnum.dirtmap,          ModelMaterialEnum.weighted_dirtmap,
                ModelMaterialEnum.decal_dirtmap,    ModelMaterialEnum.weighted_decal_dirtmap];

            if (types.Contains(rmvMaterial.MaterialId))
                return true;
            return false;
        }
    }
}
