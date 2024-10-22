using Microsoft.Xna.Framework;

namespace GameWorld.Core.Rendering
{
    public record CommonShaderParameters(
        Matrix View,
        Matrix Projection,
        Vector3 CameraPosition,
        Vector3 CameraLookAt,
        float EnvLightRotationsRadians_Y,
        float DirLightRotationRadians_X,
        float DirLightRotationRadians_Y,
        float LightIntensityMult,
        Vector3[] FactionColours
        );

}
