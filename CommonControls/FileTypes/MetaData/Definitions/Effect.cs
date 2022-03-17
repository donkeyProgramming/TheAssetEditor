using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("EFFECT", 11)]
    public class Effect_v11 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(6, "normal(0), ability_aura(1), ability_weapon(2)")]
        public int EffectType { get; set; }

        [MetaDataTag(7)]
        public bool Tracking { get; set; }

        [MetaDataTag(8, "Does the effect crash with the terrain?")]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(9)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(10)]
        public Vector3 Position { get; set; } = new Vector3();

        [MetaDataTag(11, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(12, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }

        [MetaDataTag(13, "Scale of the effect")]
        public float Scale { get; set; } = 1;
    }

    [MetaData("EFFECT", 12)]
    public class Effect_v12 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(6, "normal(0), ability_aura(1), ability_weapon(2)")]
        public int EffectType { get; set; }

        [MetaDataTag(7)]
        public bool Tracking { get; set; }

        [MetaDataTag(8, "Does the effect crash with the terrain?")]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(9)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(10)]
        public bool UnknownBool { get; set; }

        [MetaDataTag(11)]
        public Vector3 Position { get; set; } = new Vector3();

        [MetaDataTag(12, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(13, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }

        [MetaDataTag(14, "Scale of the effect")]
        public float Scale { get; set; } = 1;
    }
}
