using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("BLOOD", 11)]
    public class Blood : MetaEntryBase
    {
        [MetaDataTag(5, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; }

        [MetaDataTag(6)]
        public int Unk { get; set; }

        [MetaDataTag(7)]
        public bool Tracking { get; set; }

        [MetaDataTag(8)]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(9)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(10)]
        public Vector3 Position { get; set; }

        [MetaDataTag(11, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; }


        [MetaDataTag(12, "Bone the effect is attached to")]
        public int NodeIndex { get; set; }

        [MetaDataTag(13, "Scale of the effect")]
        public float Scale { get; set; }
    }
}
