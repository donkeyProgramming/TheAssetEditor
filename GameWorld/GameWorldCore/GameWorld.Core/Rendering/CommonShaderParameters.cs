using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        Vector3[] FactionColours,
        Vector3 LightColour,
        float ViewportHeight = 0,
        float ViewportWidth = 0
        )
    {
        /// <summary>Multiplies the model's colour and is its alpha: a rigid particle's colour off its chain. White and opaque by default.</summary>
        public Vector4 ModelColour { get; init; } = Vector4.One;

        /// <summary>The VFX preview's light particles this frame, which light the solid scene as well as the ground; the shaders take
        /// the first <see cref="MaxPointLights"/>.</summary>
        public IReadOnlyList<PointLight> PointLights { get; init; } = [];

        // A Bale Taurus standing carries 35.
        public const int MaxPointLights = 64;

        /// <summary>Hands an effect that takes them (PointLightCount, PointLights, PointLightColours) the first
        /// <see cref="MaxPointLights"/> lights.</summary>
        public static void SetPointLights(Effect effect, IReadOnlyList<PointLight> lights)
        {
            if (effect.Parameters["PointLightCount"] is not { } count)
                return;
            var first = lights.Take(MaxPointLights).ToArray();
            count.SetValue(first.Length);
            if (first.Length == 0)
                return;
            effect.Parameters["PointLights"].SetValue(first.Select(x => new Vector4(x.Position, x.Radius)).ToArray());
            effect.Parameters["PointLightColours"].SetValue(first.Select(x => new Vector4(x.Colour, 1)).ToArray());
        }

        /// <summary>The solid scene's view depth per pixel, which soft particles fade against; null when the engine drew none.</summary>
        public Texture2D? SceneDepth { get; init; }
    }

    /// <summary>A light particle in the world: where it is, how far it reaches and its colour. Shaders/Pbr light a model with it as
    /// Shaders/Particle's LightPS lights the ground under it, falling off as (1 - d^2/r^2)^2.</summary>
    public readonly record struct PointLight(Vector3 Position, float Radius, Vector3 Colour);

}
