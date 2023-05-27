using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("CREW_LOCATION", 2)]
    public class CrewLocation_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v2 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v2 { get; set; }
        
        [MetaDataTag(6)]
        public Vector3 Position{ get; set; }

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation{ get; set; }
    }
    
    [MetaData("CREW_LOCATION", 10)]
    public class CrewLocation_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0_v2 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt1_v2 { get; set; }


        [MetaDataTag(7)]
        public Vector3 Position{ get; set; }

        [MetaDataTag(8, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation{ get; set; }

        [MetaDataTag(9)]
        public int UnknownInt0_v10 { get; set; }

        [MetaDataTag(10)]
        public int UnknownInt1_v10 { get; set; }
    }
}
