using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
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

        public Matrix ModelMatrix{ get; set; }

        public float LightIntensityMult { get; set; }

        public void Apply(Effect effect, ResourceLibrary _)
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

        public void Initialize(WsModelFile wsModelFile, RmvModel model) { }
    }
}
