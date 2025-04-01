using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;

namespace Shared.GameFormats.RigidModel.Types
{
    public enum TextureType
    {
        Diffuse = 0,
        Normal = 1,
        Mask = 3,
        Ambient_occlusion = 5,
        Tiling_dirt_uv2 = 7,
        Skin_mask = 10,
        Specular = 11,
        Gloss = 12,
        Decal_dirtmap = 13,
        Decal_dirtmask = 14,
        Decal_mask = 15,
        Diffuse_damage = 17,
        BaseColour = 27,
        MaterialMap = 29,

        // Items below are not in the RMV2 file spec, but used by the WsModel Material system
        Blood                   = 1001,
        Distortion              = 1002,
        DistortionNoise         = 1003,
        Emissive                = 1004,
        EmissiveDistortion      = 1005
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{TexureType} - {Path}")]
    public struct RmvTexture
    {
        public TextureType TexureType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        byte[] _path;

        public string Path
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_path, 0, 256, out var value, out _);
                if (result == false)
                    throw new Exception();
                return StringSanitizer.FixedString(value);
            }
            set
            {
                _path = new byte[256];
                for (var i = 0; i < 256; i++)
                    _path[i] = 0;

                var byteValues = Encoding.UTF8.GetBytes(value);
                for (var i = 0; i < byteValues.Length; i++)
                {
                    _path[i] = byteValues[i];
                }
            }
        }

        public RmvTexture Clone() => new() { TexureType = TexureType, _path = _path };
    }
}
