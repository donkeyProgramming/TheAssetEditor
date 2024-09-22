namespace Shared.GameFormats.RigidModel
{
    public enum ModelMaterialEnum : ushort
    {
        bow_wave = 22,
        non_renderable = 26,
        texture_combo_vertex_wind = 29,
        texture_combo = 30,
        decal_waterfall = 31,
        standard_simple = 32,
        campaign_trees = 34,
        point_light = 38,
        static_point_light = 45,
        debug_geometry = 46,
        custom_terrain = 49,
        weighted_cloth = 58,
        cloth = 60,
        collision = 61,
        collision_shape = 62,
        tiled_dirtmap = 63,
        ship_ambientmap = 64,
        weighted = 65,
        projected_decal = 67,
        default_type = 68,
        grass = 69,
        weighted_skin = 70,
        decal = 71,
        decal_dirtmap = 72,
        dirtmap = 73,
        tree = 74,
        tree_leaf = 75,
        weighted_decal = 77,
        weighted_decal_dirtmap = 78,
        weighted_dirtmap = 79,
        weighted_skin_decal = 80,
        weighted_skin_decal_dirtmap = 81,
        weighted_skin_dirtmap = 82,
        water = 83,
        unlit = 84,
        weighted_unlit = 85,
        terrain_blend = 86,
        projected_decal_v2 = 87,
        ignore = 88,
        tree_billboard_material = 89,
        water_displace_volume = 91,
        rope = 93,
        campaign_vegetation = 94,
        projected_decal_v3 = 95,
        weighted_texture_blend = 96,
        projected_decal_v4 = 97,
        global_terrain = 98,
        decal_overlay = 99,
        alpha_blend = 100,
        TerrainTiles = 101,

        Unkown = 10000,
    };

    public static class ModelMaterialEnumHelper
    {
        public static List<ModelMaterialEnum> GetAllWeightedMaterials()
        {
            var enumValues = Enum.GetValues(typeof(ModelMaterialEnum)).Cast<ModelMaterialEnum>();
            var weightedMaterials = enumValues.Where(x => x.ToString().Contains("weighted", StringComparison.InvariantCultureIgnoreCase)).ToList();
            weightedMaterials.Add(ModelMaterialEnum.default_type);
            weightedMaterials.Add(ModelMaterialEnum.decal);
            weightedMaterials.Add(ModelMaterialEnum.dirtmap);
            weightedMaterials.Add(ModelMaterialEnum.decal_dirtmap);
            return weightedMaterials;
        }

        public static UiVertexFormat GetToolVertexFormat(VertexFormat format)
        {
            return format switch
            {
                VertexFormat.Static => UiVertexFormat.Static,
                VertexFormat.Collision_Format => UiVertexFormat.Static,
                VertexFormat.Weighted => UiVertexFormat.Weighted,
                VertexFormat.Cinematic => UiVertexFormat.Cinematic,
                VertexFormat.Position16_bit => UiVertexFormat.Static,
                VertexFormat.CustomTerrain => UiVertexFormat.Static,
                VertexFormat.CustomTerrain2 => UiVertexFormat.Static,
                _ => throw new Exception("Unknown vertex type")
            };
        }
    }

    public enum AlphaMode : int
    {
        Opaque = 0,
        Transparent = 1,
    };

    public enum VertexFormat : uint
    {
        Unknown = 99,
        Static = 0,
        Collision_Format = 1,
        Weighted = 3,
        Cinematic = 4,

        Position16_bit,
        CustomTerrain,

        CustomTerrain2 = 13,
    };

    public enum UiVertexFormat : uint
    {
        Unknown = 99,
        Static = 0,
        Weighted = 3,
        Cinematic = 4,
    };
}
