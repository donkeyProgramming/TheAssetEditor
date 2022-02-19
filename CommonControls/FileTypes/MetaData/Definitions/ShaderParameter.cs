using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SHADER_PARAMETER", 11)]
    public class ShaderParameter_v11 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "")]
        public float StartValue { get; set; }

        [MetaDataTag(6, "")]
        public float EndValue { get; set; }

        [MetaDataTag(7, "")]
        public string ParameterName { get; set; } = "emissive_tint";
    }

    [MetaData("SHADER_PARAMETER", 12)]
    public class ShaderParameter_v12 : ShaderParameter_v11
    {
        [MetaDataTag(8, "")]
        public float Unk0 { get; set; }

        [MetaDataTag(9, "")]
        public float Unk1 { get; set; }
    }
}
