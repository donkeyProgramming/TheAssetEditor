// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("CREW_LOCATION", 2)]
    public class CrewLocation_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v2 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v2 { get; set; }

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; }
    }

    [MetaData("CREW_LOCATION", 3)]
    public class CrewLocation_v3 : CrewLocation_v2
    {
        [MetaDataTag(8)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(9)]
        public int UnknownInt1_v3 { get; set; }
    }

    [MetaData("CREW_LOCATION", 10)]
    public class CrewLocation_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0_v2 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt1_v2 { get; set; }


        [MetaDataTag(7)]
        public Vector3 Position { get; set; }

        [MetaDataTag(8, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(9)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(10)]
        public int UnknownInt1_v3 { get; set; }
    }
}
