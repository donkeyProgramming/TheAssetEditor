using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SPLASH_ATTACK", 10)]
    public class SplashAttack_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "cone/circle (0), corridor (1)")]
        public int AoeShape { get; set; }

        [MetaDataTag(6)]
        public Vector3 StartPosition { get; set; } = Vector3.Zero;
        
        [MetaDataTag(7)]
        public Vector3 EndPosition { get; set; } = Vector3.Zero;

        [MetaDataTag(8, "Min = 0, Max = 20")]
        public float WidthForCorridor { get; set; }
        
        [MetaDataTag(9, "Min = 0, Max = 360")]
        public float AngleForCone { get; set; }
        
        [MetaDataTag(10, "Min = 0, Max = 10000")]
        public float ImpactMassInKg { get; set; }
        
        [MetaDataTag(11, "Min = 0, Max = 100")]
        public float ImpactSpeed { get; set; }

        [MetaDataTag(12)]
        public string GroupSound { get; set; } = "";
    }


    [MetaData("SPLASH_ATTACK", 11)]
    public class SplashAttack_v11 : SplashAttack_v10
    {
        [MetaDataTag(13, "Min = 0, Max = 1")]
        public float StrengthAtEnd { get; set; }
    }
}
