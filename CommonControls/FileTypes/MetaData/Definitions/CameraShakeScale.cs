using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("CAMERA_SHAKE_SCALE", 10)]
    public class CameraShakeScale : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value{ get; set; }
    }
}
