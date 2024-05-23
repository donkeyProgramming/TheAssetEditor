// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using SharedCore.ByteParsing;

namespace CommonControls.FileTypes.RigidModel.Vertex.Formats
{
    public class Weighted4VertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Cinematic;
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {
            if (rmvVersion == RmvVersionEnum.RMV2_V8)
                return (uint)ByteHelper.GetSize<Data>() + 4;
            else
                return (uint)ByteHelper.GetSize<Data>();
        }

        public bool ForceComputeNormals => false;

        public CommonVertex Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize)
        {
            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var vertexData = ByteHelper.ByteArrayToStructure<DataWithColour>(buffer, offset);

                var vertex = new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat(vertexData.position).ToVector4(1),
                    Normal = VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3(),
                    BiNormal = VertexLoadHelper.CreatVector4Byte(vertexData.biNormal).ToVector3(),
                    Tangent = VertexLoadHelper.CreatVector4Byte(vertexData.tangent).ToVector3(),

                    Uv = VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),
                    Colour = VertexLoadHelper.CreatVector4Byte(vertexData.colour).ToVector4(),

                    BoneIndex = new byte[] { vertexData.boneIndex[0], vertexData.boneIndex[1], vertexData.boneIndex[2], vertexData.boneIndex[3] },
                    BoneWeight = new float[] { vertexData.boneWeight[0] / 255.0f, vertexData.boneWeight[1] / 255.0f, vertexData.boneWeight[2] / 255.0f, vertexData.boneWeight[3] / 255.0f },
                    WeightCount = 4
                };

                return vertex;
            }
            else
            {
                var vertexData = ByteHelper.ByteArrayToStructure<Data>(buffer, offset);

                var vertex = new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat(vertexData.position).ToVector4(1),
                    Normal = VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3(),
                    BiNormal = VertexLoadHelper.CreatVector4Byte(vertexData.biNormal).ToVector3(),
                    Tangent = VertexLoadHelper.CreatVector4Byte(vertexData.tangent).ToVector3(),

                    Uv = VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                    Colour = new Vector4(0, 0, 0, 1),

                    BoneIndex = new byte[] { vertexData.boneIndex[0], vertexData.boneIndex[1], vertexData.boneIndex[2], vertexData.boneIndex[3] },
                    BoneWeight = new float[] { vertexData.boneWeight[0] / 255.0f, vertexData.boneWeight[1] / 255.0f, vertexData.boneWeight[2] / 255.0f, vertexData.boneWeight[3] / 255.0f },
                    WeightCount = 4
                };

                return vertex;
            }
        }


        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 4 || vertex.BoneIndex.Length != 4 || vertex.BoneWeight.Length != 4)
                throw new Exception($"Unexpected vertex weights for {Type}");

            var newPos = vertex.Position;
            newPos.W = 0;

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var typedVert = new DataWithColour()
                {
                    position = VertexLoadHelper.CreatePositionVector4(newPos),
                    boneIndex = vertex.BoneIndex.ToArray(),
                    boneWeight = vertex.BoneWeight.Select(x => (byte)(x * 255.0f)).ToArray(),
                    normal = VertexLoadHelper.CreateNormalVector3(vertex.Normal),

                    colour = VertexLoadHelper.Create4BytesFromVector4(vertex.Colour),
                    uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),

                    biNormal = VertexLoadHelper.CreateNormalVector3(vertex.BiNormal),
                    tangent = VertexLoadHelper.CreateNormalVector3(vertex.Tangent),
                };
                return ByteHelper.GetBytes(typedVert);
            }
            else
            {
                var typedVert = new Data()
                {
                    position = VertexLoadHelper.CreatePositionVector4(newPos),
                    boneIndex = vertex.BoneIndex.ToArray(),
                    boneWeight = vertex.BoneWeight.Select(x => (byte)(x * 255.0f)).ToArray(),
                    normal = VertexLoadHelper.CreateNormalVector3(vertex.Normal),

                    uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),

                    biNormal = VertexLoadHelper.CreateNormalVector3(vertex.BiNormal),
                    tangent = VertexLoadHelper.CreateNormalVector3(vertex.Tangent),
                };
                var bytes = ByteHelper.GetBytes(typedVert);
                return bytes;
            }
        }

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

        public struct DataWithColour // 36
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

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] colour;     // 4 x 1
        }
    }
}
