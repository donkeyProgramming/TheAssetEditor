using Microsoft.Xna.Framework;

namespace GameWorld.Core.Rendering
{
    public class CommonShaderParameters
    {
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }
        public Vector3 CameraPosition { get; set; }
        public Vector3 CameraLookAt { get; set; }
        public float EnvLightRotationsRadians_Y { get; set; }
        public float DirLightRotationRadians_X { get; set; }
        public float DirLightRotationRadians_Y { get; set; }

        public float LightIntensityMult { get; set; }
    }

    public enum RenderFormats
    {
        SpecGloss,
        MetalRoughness
    }
}
