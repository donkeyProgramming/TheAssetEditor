// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("SC_RADIUS", 10)]
    public class ScRadius_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }

    [MetaData("SC_HEIGHT", 10)]
    public class ScHeigt_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }

    [MetaData("SC_RATIO", 10)]
    public class ScRatio_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
