using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Rendering
{
    internal static class CommonShaderParameterBuilder
    {
        public static CommonShaderParameters Build(ArcBallCamera camera, SceneLightParametersStore sceneLightParameters)
        {
            var commonShaderParameters = new CommonShaderParameters()
            {
                Projection = camera.ProjectionMatrix,
                View = camera.ViewMatrix,
                CameraPosition = camera.Position,
                CameraLookAt = camera.LookAt,
                EnvLightRotationsRadians_Y = MathHelper.ToRadians(sceneLightParameters.EnvLightRotationDegrees_Y),
                DirLightRotationRadians_X = MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_X),
                DirLightRotationRadians_Y = MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_Y),
                LightIntensityMult = sceneLightParameters.LightIntensityMult
            };
            return commonShaderParameters;
        }
    }
}
