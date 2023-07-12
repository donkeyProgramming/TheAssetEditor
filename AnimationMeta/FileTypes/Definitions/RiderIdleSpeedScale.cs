// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AnimationMeta.FileTypes.Parsing;

namespace AnimationMeta.FileTypes.Definitions
{
    [MetaData("RIDER_IDLE_SPEED_SCALE", 10)]
    public class RiderIdleSpeedScale : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Likely speeds up/slows down rider animations.")]
        public float AnimationSpeedScale { get; set; }
    }
}
