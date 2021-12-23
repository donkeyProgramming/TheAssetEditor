using Filetypes.ByteParsing;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonControls.FileTypes.RigidModel.Types
{

    public enum TexureType
    {
        Diffuse = 0,
        Diffuse_alternative = 27,
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
        Diffuse_damage = 17
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RmvTexture
    {
        public TexureType TexureType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        byte[] _path;

        public string Path
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_path, 0, 256, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
            set
            {
                _path = new byte[256];
                for (int i = 0; i < 256; i++)
                    _path[i] = 0;

                var byteValues = Encoding.UTF8.GetBytes(value);
                for (int i = 0; i < byteValues.Length; i++)
                {
                    _path[i] = byteValues[i];
                }
            }
        }
    }
}
