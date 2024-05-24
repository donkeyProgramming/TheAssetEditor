// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("RIDER_CUSTOM_ANIMATION", 10)]
    public class RiderCustomAnimation_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int Unk0 { get; set; }

        [MetaDataTag(6)]
        public int Unk1 { get; set; }

        [MetaDataTag(7)]
        public int Unk2 { get; set; }

    }
}
