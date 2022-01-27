using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("RIDER_IDLE_SPEED_SCALE", 10)]
    public class RiderIdleSpeedScale : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Likely speeds up/slows down rider animations.")]
        public float AnimationSpeedScale { get; set; }
    }
}
