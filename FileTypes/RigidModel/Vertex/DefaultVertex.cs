using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Vertex
{
    public class DefaultVertex : BaseVertex
    {
        public struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] unknown0;     // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] unknown1;     // 2 x 2
        }

        Data _data;

        public DefaultVertex(Data data)
        {
            _data = data;

            Postition = CreatVector4HalfFloat(_data.position);
            Uv = CreatVector2HalfFloat(_data.uv);
            Normal = CreatVector4Byte(_data.normal);
            BiNormal = CreatVector4Byte(_data.biNormal);
            Tangent = CreatVector4Byte(_data.tangent);

            BoneIndex = null;
            BoneWeight = null;
        }
    }
}
