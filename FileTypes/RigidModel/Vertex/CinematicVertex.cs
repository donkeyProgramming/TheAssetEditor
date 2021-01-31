using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Vertex
{
    public class CinematicVertex : BaseVertex
    {
        public struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] boneIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] boneWeight;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1

        }

        Data _data;

        public CinematicVertex(Data data)
        {
            _data = data;

            Postition = CreatVector4HalfFloat(_data.position);
            Uv = CreatVector2HalfFloat(_data.uv);
            Normal = CreatVector4Byte(_data.normal);
            BiNormal = CreatVector4Byte(_data.biNormal);
            Tangent = CreatVector4Byte(_data.tangent);

            BoneIndex = new byte[] { _data.boneIndex[0], _data.boneIndex[1], _data.boneIndex[2], _data.boneIndex[3] };
            BoneWeight = new float[] { _data.boneWeight[0] / 255.0f, _data.boneWeight[1] / 255.0f, _data.boneWeight[2] / 255.0f, _data.boneWeight[3] / 255.0f };
        }
    }
}
