using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Rendering
{
    internal static class CommonShaderParameterBuilder
    {
        public static CommonShaderParameters Build(ArcBallCamera camera, SceneRenderParametersStore sceneLightParameters)
        {
            var commonShaderParameters = new CommonShaderParameters(
                 camera.ViewMatrix,
                camera.ProjectionMatrix,
                camera.Position,
                camera.LookAt,

                MathHelper.ToRadians(sceneLightParameters.EnvLightRotationDegrees_Y),
                MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_X),
                MathHelper.ToRadians(sceneLightParameters.DirLightRotationDegrees_Y),
                sceneLightParameters.LightIntensityMult,

                [sceneLightParameters.FactionColour0, sceneLightParameters.FactionColour1, sceneLightParameters.FactionColour2]);

            return commonShaderParameters;
        }
    }
}
