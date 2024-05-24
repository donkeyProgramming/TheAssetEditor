// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("BLEND_OVERRIDE", 10)]
    public class BlendOverride_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float BlendOutTime { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7, "blend_in_default, blend_out_default, blend_in_crossfade, blend_out_crossfade ,blend_in_time,blend_out_time")]
        public int BlendMethod { get; set; }
    }

    [MetaData("BLEND_OVERRIDE", 11)]
    public class BlendOverride_v11 : BlendOverride_v10
    {
        [MetaDataTag(8)] // some kind of scaling?
        public float UnknownFloat_v11 { get; set; }
    }


    [MetaData("BLEND_OVERRIDE", 11, MetaDataAttributePriority.Low)]
    public class BlendOverride_v11_Troy : BlendOverride_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)] // some kind of scaling?
        public byte UnknownByte_v11_troy { get; set; }
    }
}
