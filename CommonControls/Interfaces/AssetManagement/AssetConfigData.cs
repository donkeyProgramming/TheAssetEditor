namespace CommonControls.Interfaces.AssetManagement
{
    public class AnimationSettingStruct
    {
        public float Fps { get; set; }
        public float FrameCount { get; set; }
        public float TimeEnd { get; set; }
    }

    enum UnitEnum
    {
        Meters,
        Centimeters,
        Millimeters,
        Inches,
        Feet,
        Yards,
        Miles,
        Kilometers,
        MilesScandinavian,
        Decimeters,
        Decameters,
        Hectometers,
        Microinches,
        Mil,
        Custom
    }

    /// <summary>
    /// Configurations data, for C# mesh processors, and ultimately the (FBX) SDK saving the scene (.fbx) to disk
    /// </summary>
    public class AssetConfigData
    {
        float Scale { get; set; } = 1.0f;
        UnitEnum SaveAsUnit { get; set; } = UnitEnum.Centimeters;
        public AnimationSettingStruct AnimationSettings { get; set; } = new AnimationSettingStruct();
    }
}
