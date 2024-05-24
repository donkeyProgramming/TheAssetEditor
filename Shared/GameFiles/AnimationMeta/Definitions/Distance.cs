// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("DISTANCE", 2)]
    public class Distance_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public float Value { get; set; }
    }

    [MetaData("DISTANCE", 10)]
    public class Distance_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
