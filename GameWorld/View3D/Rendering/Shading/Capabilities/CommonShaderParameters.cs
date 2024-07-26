using System.Collections.Generic;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;


namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    internal class CommonShaderParametersCapability : ICapability
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

        public void Apply(Effect effect)
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
    }

    public class TextureInput
    {
        public string? TexturePath { get; set; }
        public bool UseTexture { get; set; }
        public TextureType Type { get; set; }

        public TextureInput(TextureType type)
        {
            Type = type;
            UseTexture = false;
            TexturePath = null;
        }

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            var useTextureParam = ShaderParameterHelper.UseTextureTypeToParamName[Type];
            var textureParam = ShaderParameterHelper.TextureTypeToParamName[Type];
            effect.Parameters[useTextureParam].SetValue(UseTexture);

            if (UseTexture)
            {
                var texture = resourceLibrary.LoadTexture(TexturePath);
                effect.Parameters[textureParam].SetValue(texture);
            }

            //if (Texture == null || ApplyTexture == false)
            //{
            //    effect.Parameters[useTextureParam].SetValue(false);
            //}
            //else
            //{
            //    var textureParam = ShaderParameterHelper.TextureTypeToParamName[Type];
            //    effect.Parameters[useTextureParam].SetValue(ApplyTexture);
            //    effect.Parameters[textureParam].SetValue(Texture);
            //}

        }
    }

  
    internal class SharedCapability : ICapability
    {
        public bool ApplyAnimation{ get; set; }
        public bool UseAlpha { get; set; }
        public bool AnimationInformation { get; set; }
        public Matrix[]? AnimationTransforms { get; set; }
        public int AnimationWeightCount { get; set; }

        public TextureInput BaseColour { get; set; } = new TextureInput(TextureType.BaseColour);
        public TextureInput MaterialMap{ get; set; } = new TextureInput(TextureType.MaterialMap);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);

        public Dictionary<TextureType, TextureInput> TextureMap { get; private set; } = [];

        public SharedCapability()
        {
            TextureMap[BaseColour.Type] = BaseColour;
            TextureMap[MaterialMap.Type] = MaterialMap;
            TextureMap[NormalMap.Type] = NormalMap;
            TextureMap[Mask.Type] = Mask;
        }

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);

            // Apply Animation 
            effect.Parameters["doAnimation"].SetValue(ApplyAnimation);
            effect.Parameters["WeightCount"].SetValue(AnimationWeightCount);
            effect.Parameters["tranforms"].SetValue(AnimationTransforms);

            BaseColour.Apply(effect, resourceLibrary);
            MaterialMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            Mask.Apply(effect, resourceLibrary);
        }
    }
}
