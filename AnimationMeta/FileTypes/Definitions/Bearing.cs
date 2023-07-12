// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AnimationMeta.FileTypes.Parsing;

namespace AnimationMeta.FileTypes.Definitions
{
    [MetaData("BEARING", 10)]
    public class Bearing_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Unk { get; set; }
    }
}
