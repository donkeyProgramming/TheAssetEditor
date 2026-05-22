using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Rendering
{
    internal static class CommonShaderParameterBuilder
    {
        public static CommonShaderParameters Build(ArcBallCamera camera, SceneRenderParametersStore sceneLightParameters, float viewportWidth, float viewportHeight)
        {
            // Light follows camera rotation for better model visibility
            float dirLightRotX = MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_X) + camera.Pitch;
            float dirLightRotY = MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_Y) + camera.Yaw;
            float envLightRotY = MathHelper.ToRadians(sceneLightParameters.EnvLightRotationDegrees_Y) + camera.Yaw;

            var commonShaderParameters = new CommonShaderParameters(
                camera.ViewMatrix,
                camera.ProjectionMatrix,
                camera.Position,
                camera.LookAt,

                envLightRotY,
                dirLightRotX,
                dirLightRotY,
                sceneLightParameters.LightIntensityMult,

                [sceneLightParameters.FactionColour0, sceneLightParameters.FactionColour1, sceneLightParameters.FactionColour2],
                sceneLightParameters.LightColour,
                viewportHeight,
                viewportWidth
                );

            return commonShaderParameters;
        }
    }
}
