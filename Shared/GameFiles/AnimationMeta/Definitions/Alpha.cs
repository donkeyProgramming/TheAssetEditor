// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("ALPHA", 10)]
    public class Alpha_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "This might be a %, 0 = invisible and 1 = visible.")]
        public float DesiredAlpha { get; set; }
    }
}
