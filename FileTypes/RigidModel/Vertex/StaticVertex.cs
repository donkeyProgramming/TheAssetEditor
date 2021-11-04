using Filetypes.RigidModel.Transforms;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Vertex
{
    public class StaticVertex : BaseVertex
    {
        public struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uvExtra;     // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] RGBA;     // 4 x 1
        }

        public Data _data;

        public StaticVertex(Data data)
        {
            _data = data;
            CreateFromData(_data);
           // Fix();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(ByteHelper.GetBytes(_data));
        }

        public StaticVertex(RmvVector4 position, RmvVector2 uv, RmvVector3 normal, RmvVector3 biNormal, RmvVector3 tanget)
        {
            _data = new Data()
            {
                position = CreatePositionVector4(position),
                uv = CreatePositionVector2(uv),
                uvExtra = CreatePositionVector2(new RmvVector2(0,0)),
                normal = CreateNormalVector3(normal),
                biNormal = CreateNormalVector3(biNormal),
                tangent = CreateNormalVector3(tanget),
                RGBA = new byte[4] { 0,0,0,0}
            };

            CreateFromData(_data);
        }

        void CreateFromData(Data data)
        {
            Postition = CreatVector4HalfFloat(data.position);
            Uv = CreatVector2HalfFloat(data.uv);
            Normal = CreatVector4Byte(_data.normal);
            BiNormal = CreatVector4Byte(data.biNormal);
            Tangent = CreatVector4Byte(data.tangent);

            BoneIndex = null;
            BoneWeight = null;
        }
    }
}
