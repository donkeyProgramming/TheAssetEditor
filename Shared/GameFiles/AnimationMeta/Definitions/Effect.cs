// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    public interface IEffectMeta
    {
        public string VfxName { get; set; }
    }

    [MetaData("EFFECT", 1)]
    public class Effect_v1 : DecodedMetaEntryBase_v1, IEffectMeta
    {
        [MetaDataTag(3, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(4, "True, false or empty.")]
        public string BoolUnknown { get; set; } = "";

        [MetaDataTag(5)]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [MetaDataTag(6, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(7, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }
    }

    [MetaData("EFFECT", 2)]
    public class Effect_v2 : DecodedMetaEntryBase_v2, IEffectMeta
    {
        [MetaDataTag(4, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(5, "True, false or empty.")]
        public string BoolUnknown { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(8, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }
    }

    [MetaData("EFFECT", 3)]
    public class Effect_v3 : Effect_v2
    {
        [MetaDataTag(9, "Scale of the effect")]
        public float Scale { get; set; } = 1;
    }

    [MetaData("EFFECT", 4)]
    public class Effect_v4 : Effect_v2
    {
        //TODO: exactly same ???
    }

    [MetaData("EFFECT", 5)]
    public class Effect_v5 : Effect_v3
    {
        //TODO: exactly same ???
    }

    [MetaData("EFFECT", 7)]
    public class Effect_v7 : DecodedMetaEntryBase_v2, IEffectMeta
    {
        [MetaDataTag(4, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(5)]
        public bool Tracking { get; set; }

        [MetaDataTag(6, "Does the effect crash with the terrain?")]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(7)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(8)]
        public Vector3 Position { get; set; } = new Vector3();

        [MetaDataTag(9, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(10, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }

        [MetaDataTag(11, "Scale of the effect")]
        public float Scale { get; set; } = 1;
    }

    [MetaData("EFFECT", 11)]
    public class Effect_v11 : DecodedMetaEntryBase, IEffectMeta
    {
        [MetaDataTag(5, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(6, "normal(0), ability_aura(1), ability_weapon(2)")]
        public int EffectType { get; set; }

        [MetaDataTag(7)]
        public bool Tracking { get; set; }

        [MetaDataTag(8, "Does the effect crash with the terrain?")]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(9)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(10)]
        public Vector3 Position { get; set; } = new Vector3();

        [MetaDataTag(11, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(12, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public int NodeIndex { get; set; }

        [MetaDataTag(13, "Scale of the effect")]
        public float Scale { get; set; } = 1;
    }

    [MetaData("EFFECT", 12)]
    public class Effect_v12 : Effect_v11
    {
        // new field
        [MetaDataTag(10)]
        public bool ScalesWithParent { get; set; }  

        // override order
        [MetaDataTag(11)]
        public new Vector3 Position { get; set; } = new Vector3();

        [MetaDataTag(12, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public new Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(13, "Bone the effect is attached to, use -1 for it to just spawn and not follow animations")]
        public new int NodeIndex { get; set; }

        [MetaDataTag(14, "Scale of the effect")]
        public new float Scale { get; set; } = 1;
    }
}
