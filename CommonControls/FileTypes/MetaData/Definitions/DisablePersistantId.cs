using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{


    [MetaData("DISABLE_PERSISTENT_ID", 10)]
    public class DisablePersistantId_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value{ get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }
    }

    [MetaData("DISABLE_PERSISTENT_ID", 11)]
    public class DisablePersistantId_v11 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }

        [MetaDataTag(8)]
        public bool PositionOnly { get; set; }
    }
}
