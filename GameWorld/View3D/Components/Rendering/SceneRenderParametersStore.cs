using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Rendering
{
    public class SceneRenderParametersStore
    {
        public float EnvLightRotationDegrees_Y { get; set; } = 20;
        public float DirLightRotationDegrees_X { get; set; } = 0;
        public float DirLightRotationDegrees_Y { get; set; } = 0;
        public float LightIntensityMult { get; set; } = 1;

        public Vector3 FactionColour0 { get; set; } = Color.Red.ToVector3();
        public Vector3 FactionColour1 { get; set; } = Color.Blue.ToVector3();
        public Vector3 FactionColour2 { get; set; } = Color.White.ToVector3();
    }
}
