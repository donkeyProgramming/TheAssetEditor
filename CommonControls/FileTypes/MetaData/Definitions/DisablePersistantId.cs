using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{


    [MetaData("DISABLE_PERSISTENT_ID", 10)]
    public class DisablePersistantId_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Unk0{ get; set; }

        [MetaDataTag(6)]
        public float Unk1 { get; set; }

        [MetaDataTag(7)]
        public float Unk2 { get; set; }
    }
}
