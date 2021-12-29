using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaEntry("PROP", 10)]
    public class Prop_v10 : MetaEntryBase
    {
        [MetaDataTag(5, "Path to model. Probably has to be of the \"rigid\" vertex type, so no animated models")]
        public string ModelName { get; set; }

        [MetaDataTag(6, "Always empty")]
        public string UnknownString { get; set; }

        [MetaDataTag(7)]
        public Vector3 Position { get; set; }

        [MetaDataTag(8)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(9)]
        public int BoneId { get; set; }

        [MetaDataTag(10)]
        public int Unk_AttachMethod { get; set; }

        [MetaDataTag(11)]
        public int Unk_OverrideProp { get; set; }
    }
}
