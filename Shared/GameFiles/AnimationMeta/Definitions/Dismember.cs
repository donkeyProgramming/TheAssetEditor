// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{

    [MetaData("DISMEMBER", 2)]
    public class Dismember_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int BoneIndex { get; set; }

        [MetaDataTag(5)]
        public float BonePosition { get; set; }

        [MetaDataTag(6)]
        public Vector3 Direction { get; set; }

        [MetaDataTag(7)]
        public Vector3 Rotation { get; set; }
    }

    [MetaData("DISMEMBER", 10)]
    public class Dismember_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int BoneIndex { get; set; }

        [MetaDataTag(6)]
        public float BonePosition { get; set; }

        [MetaDataTag(7)]
        public Vector3 Direction { get; set; }

        [MetaDataTag(8)]
        public Vector3 Rotation { get; set; }
    }


    [MetaData("ALLOW_LEG_DISMEMBER", 10)]
    public class AllowLegDismember_v10 : DecodedMetaEntryBase
    {
    }
    [MetaData("ALLOW_FRONT_LEG_DISMEMBER", 10)]
    public class AllowFrontLegDismember_v10 : DecodedMetaEntryBase
    {
    }


    [MetaData("CAMPAIGN_DISMEMBER", 2)]
    public class CampaignDismember_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v2 { get; set; }

        [MetaDataTag(5)]
        public string AnimationString { get; set; } = "";
    }

}
