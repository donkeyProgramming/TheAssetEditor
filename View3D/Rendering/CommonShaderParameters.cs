using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Rendering
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
