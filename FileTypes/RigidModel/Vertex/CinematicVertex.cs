using Filetypes.RigidModel.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
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

        public Data _data;
        public ColourData? _colourData;

        public CinematicVertex(Data data, ColourData? colourData = null)
        {
            _data = data;
            _colourData = colourData;
            CreateFromData(_data);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(ByteHelper.GetBytes(_data));
            if(_colourData != null)
                writer.Write(ByteHelper.GetBytes(_colourData.Value));
        }

        public CinematicVertex(RmvVector4 position, RmvVector2 uv, RmvVector3 normal, RmvVector3 biNormal, RmvVector3 tanget, BoneInformation[] boneInformation, ColourData? colourData = null)
        {
            if (boneInformation.Length != 4)
                throw new ArgumentException();

            _data = new Data()
            {
                position = CreatePositionVector4(position),
                uv = CreatePositionVector2(uv),
                boneIndex = boneInformation.Select(x => x.BoneIndex).ToArray(),
                boneWeight = boneInformation.Select(x => (byte)(x.BoneWeight * 255.0f)).ToArray(),
                normal = CreateNormalVector3(normal),
                biNormal = CreateNormalVector3(biNormal),
                tangent = CreateNormalVector3(tanget),
            };

            _colourData = colourData;

            CreateFromData(_data);
        }

        void CreateFromData(Data data)
        {
            Postition = CreatVector4HalfFloat(data.position);
            Uv = CreatVector2HalfFloat(data.uv);
            Normal = CreatVector4Byte(data.normal);
            BiNormal = CreatVector4Byte(data.biNormal);
            Tangent = CreatVector4Byte(data.tangent);

            BoneIndex = new byte[] { data.boneIndex[0], data.boneIndex[1], data.boneIndex[2], data.boneIndex[3] };
            BoneWeight = new float[] { data.boneWeight[0] / 255.0f, data.boneWeight[1] / 255.0f, data.boneWeight[2] / 255.0f, data.boneWeight[3] / 255.0f };
        }

    }
}
