using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("CAMERA_SHAKE_POS", 10)]
    public class CameraShakePos : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public Vector3 Position{ get; set; }
    }
}
