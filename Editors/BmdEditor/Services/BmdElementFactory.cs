using Microsoft.Xna.Framework;
using Shared.GameFormats.Bmd;
using Shared.GameFormats.RigidModel.Transforms;

namespace Editors.BmdEditor.Services
{
    /// <summary>
    /// Builds default elements for the "Add" menu, each at the highest version <see cref="BmdWriter"/>
    /// writes for its type. Every record carries its own version, so it needn't match the file's others.
    /// </summary>
    public static class BmdElementFactory
    {
        private const string DefaultHeightMode = "HM_TERRAIN";

        // All-0xFF is the "no culture restriction" pattern vanilla files use; an all-zero mask would
        // hide the new element from every culture.
        private static byte[] WildcardCultureMaskBytes() => [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        private static BmdComponentFlags AllSeasonsFlags() => new()
        {
            FlagVersion = 4,
            SeasonSpring = true,
            SeasonSummer = true,
            SeasonAutumn = true,
            SeasonWinter = true,
        };

        public static PropInfo CreateProp(string rmv2Path, bool isDecal = false) => new()
        {
            PropInfoVersion = 25,
            Rmv2Path = rmv2Path,
            Transform = Matrix.Identity,
            IsDecal = isDecal,
            Flags = AllSeasonsFlags(),
            HeightMode = "BHM_PARENT",
            CultureMask = WildcardCultureMaskBytes(),
            CastsShadow = true,
            VisibleInsideSnowRegion = true,
            VisibleOutsideSnowRegion = true,
            VisibleInsideDestructionRegion = true,
            VisibleOutsideDestructionRegion = true,
            VisibleWithoutShroud = true,
        };

        public static VfxInfo CreateVfx(string vfxString) => new()
        {
            VfxInfoVersion = 10,
            VfxString = vfxString,
            Transform = Matrix.Identity,
            Flags = AllSeasonsFlags(),
            HeightMode = DefaultHeightMode,
            CultureMask = WildcardCultureMaskBytes(),
            Autoplay = true,
            VisibleWithoutShroud = true,
        };

        public static PointLightInfo CreatePointLight() => new()
        {
            PointLightInfoVersion = 7,
            Position = new RmvVector3(0, 0, 0),
            Radius = 10,
            Red = 1,
            Green = 1,
            Blue = 1,
            ColorScale = 1,
            HeightMode = DefaultHeightMode,
            Flags = AllSeasonsFlags(),
        };

        public static SpotLightInfo CreateSpotLight() => new()
        {
            Version = 8,
            Position = new RmvVector3(0, 0, 0),
            QuartX = 0,
            QuartY = 0,
            QuartZ = 0,
            QuartW = 1,
            Length = 10,
            InnerAngleRadians = 0.3f,
            OuterAngleRadians = 0.6f,
            IntensityRed = 1,
            IntensityGreen = 1,
            IntensityBlue = 1,
            Falloff = 1,
            HeightMode = DefaultHeightMode,
            Flags = AllSeasonsFlags(),
        };

        public static SoundInfo CreateSound(string soundString) => new()
        {
            Version = 10,
            SoundString = soundString,
            TypeString = "SST_POINT",
            CoordList = [new RmvVector3(0, 0, 0)],
            HeightMode = DefaultHeightMode,
            CultureMask = new CultureMask { RawBytes = WildcardCultureMaskBytes() },
        };

        public static PolyMeshInfo CreatePolyMesh(string materialString) => new()
        {
            PolyMeshVersion = 4,
            VertexList = [new RmvVector3(0, 0, 0), new RmvVector3(1, 0, 0), new RmvVector3(0, 1, 0)],
            TriangleList = [0, 1, 2],
            MaterialString = materialString,
            HeightMode = DefaultHeightMode,
            Flags = AllSeasonsFlags(),
            Transform = Matrix.Identity,
            VisibleInsideSnowRegion = true,
            VisibleOutsideSnowRegion = true,
            VisibleInsideDestructionRegion = true,
            VisibleOutsideDestructionRegion = true,
            VisibleWithoutShroud = true,
        };

        public static LightProbeInfo CreateLightProbe() => new()
        {
            Version = 3,
            Position = new RmvVector3(0, 0, 0),
            OuterRadius = 10,
            InnerRadius = 5,
            HeightMode = DefaultHeightMode,
        };

        public static TerrainHoleTriangleInfo CreateTerrainHole() => new()
        {
            TerrainHoleVersion = 3,
            FirstVert = new RmvVector3(0, 0, 0),
            SecondVert = new RmvVector3(1, 0, 0),
            ThirdVert = new RmvVector3(0, 1, 0),
            HeightMode = DefaultHeightMode,
            Flags = AllSeasonsFlags(),
        };

        public static CscInfo CreateCsc(string sceneFile) => new()
        {
            Version = 12,
            SceneFile = sceneFile,
            Transform = Matrix.Identity,
            HeightMode = DefaultHeightMode,
            Autoplay = true,
            VisibleWithoutShroud = true,
        };
    }
}
