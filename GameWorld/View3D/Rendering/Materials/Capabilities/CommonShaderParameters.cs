using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class CommonShaderParametersCapability : ICapability
    {
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }
        public Vector3 CameraPosition { get; set; }
        public Vector3 CameraLookAt { get; set; }
        public float EnvLightRotationsRadians_Y { get; set; }
        public float DirLightRotationRadians_X { get; set; }
        public float DirLightRotationRadians_Y { get; set; }

        public Matrix ModelMatrix { get; set; }

        public float LightIntensityMult { get; set; }

        public void Apply(Effect effect, IScopedResourceLibrary _)
        {
            effect.Parameters["View"].SetValue(View);
            effect.Parameters["Projection"].SetValue(Projection);
            effect.Parameters["EnvMapTransform"].SetValue(Matrix.CreateRotationY(EnvLightRotationsRadians_Y));
            effect.Parameters["DirLightTransform"].SetValue(Matrix.CreateRotationY(DirLightRotationRadians_Y) * Matrix.CreateRotationX(DirLightRotationRadians_X));
            effect.Parameters["LightMult"].SetValue(LightIntensityMult);
            effect.Parameters["World"].SetValue(ModelMatrix);
            effect.Parameters["CameraPos"].SetValue(CameraPosition);
        }

        public void Assign(CommonShaderParameters parameters, Matrix modelMatrix)
        {
            ModelMatrix = modelMatrix;

            View = parameters.View;
            Projection = parameters.Projection;
            CameraPosition = parameters.CameraPosition;
            CameraLookAt = parameters.CameraLookAt;
            EnvLightRotationsRadians_Y = parameters.EnvLightRotationsRadians_Y;
            DirLightRotationRadians_X = parameters.DirLightRotationRadians_X;
            DirLightRotationRadians_Y = parameters.DirLightRotationRadians_Y;
            LightIntensityMult = parameters.LightIntensityMult;
        }

        public ICapability Clone()
        {
            return new CommonShaderParametersCapability()
            {
                View = View,
                Projection = Projection,
                CameraPosition = CameraPosition,
                CameraLookAt = CameraLookAt,
                EnvLightRotationsRadians_Y = EnvLightRotationsRadians_Y,
                DirLightRotationRadians_X = DirLightRotationRadians_X,
                DirLightRotationRadians_Y = DirLightRotationRadians_Y,
                ModelMatrix = ModelMatrix,
                LightIntensityMult = LightIntensityMult,
            };
        }

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            var typedCap = otherCap as CommonShaderParametersCapability;
            if (typedCap == null)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");
            return (true, "");
        }
    }
}
