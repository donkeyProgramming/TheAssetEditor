namespace Shared.GameFormats.WsModel
{
    public static class WsModelParamters
    {
        public record Instance(string Name)
        { 
            public string TemplateName { get => "TEMPLATE_ATTR_" + Name.ToUpper(); }
        }

        public static Instance Texture_BaseColour { get; } = new Instance("BASE_COLOUR_PATH");
        public static Instance Texture_Diffse { get; } = new Instance("DIFFUSE_PATH");
        public static Instance Texture_Specular { get; } = new Instance("SPECULAR_PATH");
        public static Instance Texture_Gloss { get; } = new Instance("GLOSS_PATH");
        public static Instance Texture_Mask { get; } = new Instance("MASK_PATH");
        public static Instance Texture_MaterialMap { get; } = new Instance("MATERIAL_MAP_PATH");
        public static Instance Texture_Normal { get; } = new Instance("NORMAL_PATH");
        public static Instance Texture_DistortionNoise { get; } = new Instance("DISTORTIONNOISE_PATH");
        public static Instance Texture_Distortion { get; } = new Instance("DISTORTION_PATH");
        public static Instance Texture_Blood { get; } = new Instance("BLOOOD_PATH");
        public static Instance Texture_EmissiveDistortion { get; } = new Instance("EMISSIVE_DISTORT_PATH");
        public static Instance Texture_Emissive { get; } = new Instance("EMISSIVE_PATH");

        public static Instance Blood_Scale { get; } = new Instance("blood_uv_scale");
        public static Instance Blood_Use { get; } = new Instance("receives_blood");

        public static Instance Emissive_Direction { get; } = new Instance("emissive_direction");
        public static Instance Emissive_DistortStrength { get; } = new Instance("emissive_distort_strength");
        public static Instance Emissive_FesnelStrength{ get; } = new Instance("emissive_fresnel_strength");
        public static Instance Emissive_GradientColour1 { get; } = new Instance("emissive_gradient_colour_stop_1");
        public static Instance Emissive_GradientColour2 { get; } = new Instance("emissive_gradient_colour_stop_2");
        public static Instance Emissive_GradientColour3 { get; } = new Instance("emissive_gradient_colour_stop_3");
        public static Instance Emissive_GradientColour4 { get; } = new Instance("emissive_gradient_colour_stop_4");
        public static Instance Emissive_GradientTime1 { get; } = new Instance("emissive_gradient_stop_1");
        public static Instance Emissive_GradientTime2 { get; } = new Instance("emissive_gradient_stop_2");
        public static Instance Emissive_GradientTime3 { get; } = new Instance("emissive_gradient_stop_3");
        public static Instance Emissive_GradientTime4 { get; } = new Instance("emissive_gradient_stop_4");
        public static Instance Emissive_PulseSpeed { get; } = new Instance("emissive_pulse_speed");
        public static Instance Emissive_PulseStrength { get; } = new Instance("emissive_pulse_strength");
        public static Instance Emissive_Speed { get; } = new Instance("emissive_speed");
        public static Instance Emissive_Strength { get; } = new Instance("emissive_strength");
        public static Instance Emissive_Tiling{ get; } = new Instance("emissive_tiling");
        public static Instance Emissive_Tint { get; } = new Instance("emissive_tint");
    }
}
