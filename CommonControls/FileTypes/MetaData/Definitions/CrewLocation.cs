using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("CREW_LOCATION", 10)]
    public class CrewLocation_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int Unk0 { get; set; }

        [MetaDataTag(6)]
        public int Unk1 { get; set; }


        [MetaDataTag(7)]
        public Vector3 Position{ get; set; }

        [MetaDataTag(8, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation{ get; set; }

        [MetaDataTag(9)]
        public int Unk3 { get; set; }

        [MetaDataTag(10)]
        public int Unk4 { get; set; }
    }
}
