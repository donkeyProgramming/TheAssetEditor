using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class EmissiveCapability : ICapability
    {
        public TextureInput Emissive { get; set; } = new TextureInput(TextureType.Emissive);
        public TextureInput EmissiveDistortion { get; set; } = new TextureInput(TextureType.EmissiveDistortion);

        public Vector2 EmissiveDirection { get; set; } = new Vector2(1);

        public float EmissiveDistortStrength { get; set; } = 1;
        public float EmissiveFresnelStrength { get; set; } = 1;

        public float EmissiveSpeed { get; set; } = 1;
        public float EmissivePulseSpeed { get; set; } = 1;
        public float EmissivePulseStrength { get; set; } = 1;

        // Colents, where the Alpha is used as time (0-1). RedGreenBlueTime
        public Vector3[] GradientColours { get; set; } = [new Vector3(0), new Vector3(0.25f), new Vector3(0.75f), new Vector3(1)];
        public float[] GradientTimes { get; set; } = [0, 0.33f, 0.66f, 1];

        public float EmissiveStrength { get; set; } = 1;
        public Vector2 EmissiveTiling { get; set; } = new Vector2(1);
        public Vector3 EmissiveTint { get; set; } = new Vector3(1, 0, 0);

        public void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {
            Emissive.Apply(effect, resourceLibrary);

            effect.Parameters["CapabilityFlag_ApplyEmissive"].SetValue(true);

            effect.Parameters["Emissive_Tint"].SetValue(EmissiveTint);
            effect.Parameters["Emissive_Strength"].SetValue(EmissiveStrength);
            effect.Parameters["Emissive_Tiling"].SetValue(EmissiveTiling);
            effect.Parameters["Emissive_GradientColours"].SetValue(GradientColours);
            effect.Parameters["Emissive_GradientTimes"].SetValue(GradientTimes);
            effect.Parameters["Emissive_FresnelStrength"].SetValue(EmissiveFresnelStrength);
        }

        public ICapability Clone()
        {
            return new EmissiveCapability()
            {
                Emissive = Emissive.Clone(),
                EmissiveDistortion = EmissiveDistortion.Clone(),
                EmissiveDirection = EmissiveDirection,
                EmissiveDistortStrength = EmissiveDistortStrength,
                EmissiveFresnelStrength = EmissiveFresnelStrength,
                EmissiveSpeed = EmissiveSpeed,
                EmissivePulseSpeed = EmissivePulseSpeed,
                EmissivePulseStrength = EmissivePulseStrength,
                GradientColours = [GradientColours[0], GradientColours[1], GradientColours[2], GradientColours[3]],
                GradientTimes = [GradientTimes[0], GradientTimes[1], GradientTimes[2], GradientTimes[3]],
                EmissiveStrength = EmissiveStrength,
                EmissiveTiling = EmissiveTiling,
                EmissiveTint = EmissiveTint,
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial) => EmissiveCapabilitySerializer.Initialize(this, wsModelMaterial, rmvMaterial);
        public void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) => EmissiveCapabilitySerializer.SerializeToWsModel(this, templateHandler);

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not EmissiveCapability typedCap)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(Emissive, typedCap.Emissive, nameof(Emissive), out var res0))
                return res0;
            if (!CompareHelper.Compare(EmissiveDistortion, typedCap.EmissiveDistortion, nameof(EmissiveDistortion), out var res1))
                return res1;
            if (!CompareHelper.Compare(EmissiveDirection, typedCap.EmissiveDirection, nameof(EmissiveDirection), out var res2))
                return res2;
            if (!CompareHelper.Compare(EmissiveDistortStrength, typedCap.EmissiveDistortStrength, nameof(EmissiveDistortStrength), out var res3))
                return res3;
            if (!CompareHelper.Compare(EmissiveFresnelStrength, typedCap.EmissiveFresnelStrength, nameof(EmissiveFresnelStrength), out var res4))
                return res4;
            if (!CompareHelper.Compare(EmissiveSpeed, typedCap.EmissiveSpeed, nameof(EmissiveSpeed), out var res5))
                return res5;
            if (!CompareHelper.Compare(EmissivePulseSpeed, typedCap.EmissivePulseSpeed, nameof(EmissivePulseSpeed), out var res6))
                return res6;
            if (!CompareHelper.Compare(EmissivePulseStrength, typedCap.EmissivePulseStrength, nameof(EmissivePulseStrength), out var res7))
                return res7;

            for (var i = 0; i < 4; i++)
            {
                if (!CompareHelper.Compare(GradientColours[i], typedCap.GradientColours[i], nameof(GradientColours), out var res8))
                    return res8;
                if (!CompareHelper.Compare(GradientTimes[i], typedCap.GradientTimes[i], nameof(GradientTimes), out var res9))
                    return res9;
            }

            if (!CompareHelper.Compare(EmissiveStrength, typedCap.EmissiveStrength, nameof(EmissiveStrength), out var res10))
                return res10;
            if (!CompareHelper.Compare(EmissiveTiling, typedCap.EmissiveTiling, nameof(EmissiveTiling), out var res11))
                return res11;
            if (!CompareHelper.Compare(EmissiveTint, typedCap.EmissiveTint, nameof(EmissiveTint), out var res12))
                return res12;

            return (true, "");
        }
    }

    public static class EmissiveCapabilitySerializer 
    {
        public static void Initialize(EmissiveCapability output, WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.Emissive);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.EmissiveDistortion);

            output.EmissiveDirection = CapabilityHelper.GetParameterVector2(wsModelMaterial, WsModelParamters.Emissive_Direction, Vector2.One);
            output.EmissiveDistortStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_DistortStrength, 1);
            output.EmissiveFresnelStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_FesnelStrength, 1);
            output.EmissivePulseSpeed = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_PulseSpeed, 1);
            output.EmissivePulseStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_PulseStrength, 1);
            output.EmissiveSpeed = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_Speed, 1);
            output.EmissiveStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_Strength, 1);
            output.EmissiveTint = CapabilityHelper.GetParameterVector3(wsModelMaterial, WsModelParamters.Emissive_Tint, Vector3.Zero);
            output.EmissiveTiling = CapabilityHelper.GetParameterVector2(wsModelMaterial, WsModelParamters.Emissive_Tiling, Vector2.One);

            output.GradientColours[0] = CapabilityHelper.GetParameterVector3(wsModelMaterial, WsModelParamters.Emissive_GradientColour1, Vector3.One);
            output.GradientColours[1] = CapabilityHelper.GetParameterVector3(wsModelMaterial, WsModelParamters.Emissive_GradientColour2, Vector3.One);
            output.GradientColours[2] = CapabilityHelper.GetParameterVector3(wsModelMaterial, WsModelParamters.Emissive_GradientColour3, Vector3.One);
            output.GradientColours[3] = CapabilityHelper.GetParameterVector3(wsModelMaterial, WsModelParamters.Emissive_GradientColour4, Vector3.One);

            output.GradientTimes[0] = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_GradientTime1, 0);
            output.GradientTimes[1] = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_GradientTime2, 0);
            output.GradientTimes[2] = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_GradientTime3, 0);
            output.GradientTimes[3] = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Emissive_GradientTime4, 0);
        }

        public static void SerializeToWsModel(EmissiveCapability typedCap, WsMaterialTemplateEditor templateHandler)
        {
            templateHandler.AddAttribute(WsModelParamters.Texture_Emissive.TemplateName, typedCap.Emissive);
            templateHandler.AddAttribute(WsModelParamters.Texture_EmissiveDistortion.TemplateName, typedCap.EmissiveDistortion);

            templateHandler.AddAttribute(WsModelParamters.Emissive_Direction.TemplateName, typedCap.EmissiveDirection);
            templateHandler.AddAttribute(WsModelParamters.Emissive_DistortStrength.TemplateName, typedCap.EmissiveDistortStrength);
            templateHandler.AddAttribute(WsModelParamters.Emissive_FesnelStrength.TemplateName, typedCap.EmissiveFresnelStrength);
            templateHandler.AddAttribute(WsModelParamters.Emissive_Speed.TemplateName, typedCap.EmissiveSpeed);
            templateHandler.AddAttribute(WsModelParamters.Emissive_PulseSpeed.TemplateName, typedCap.EmissivePulseSpeed);
            templateHandler.AddAttribute(WsModelParamters.Emissive_PulseStrength.TemplateName, typedCap.EmissivePulseStrength);

            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientColour1.TemplateName, typedCap.GradientColours[0]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientColour2.TemplateName, typedCap.GradientColours[1]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientColour3.TemplateName, typedCap.GradientColours[2]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientColour4.TemplateName, typedCap.GradientColours[3]);

            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientTime1.TemplateName, typedCap.GradientTimes[0]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientTime2.TemplateName, typedCap.GradientTimes[1]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientTime3.TemplateName, typedCap.GradientTimes[2]);
            templateHandler.AddAttribute(WsModelParamters.Emissive_GradientTime4.TemplateName, typedCap.GradientTimes[3]);

            templateHandler.AddAttribute(WsModelParamters.Emissive_Strength.TemplateName, typedCap.EmissiveStrength);
            templateHandler.AddAttribute(WsModelParamters.Emissive_Tiling.TemplateName, typedCap.EmissiveTiling);
            templateHandler.AddAttribute(WsModelParamters.Emissive_Tint.TemplateName, typedCap.EmissiveTint);

        }
    }
}
