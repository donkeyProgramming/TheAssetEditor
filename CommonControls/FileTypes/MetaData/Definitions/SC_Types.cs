using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SC_RADIUS", 10)]
    public class ScRadius_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }

    [MetaData("SC_HEIGHT", 10)]
    public class ScHeigt_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }

    [MetaData("SC_RATIO", 10)]
    public class ScRatio_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
