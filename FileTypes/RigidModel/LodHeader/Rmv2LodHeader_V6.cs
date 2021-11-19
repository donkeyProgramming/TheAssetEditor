using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.LodHeader
{

    public class Rmv2LodHeader_V6_Creator : ILodHeaderCreator
    {
        public uint HeaderSize => (uint)ByteHelper.GetSize(typeof(Rmv2LodHeader_V6));

        public RmvLodHeader Create(byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V6>(buffer, offset);
            return header;
        }

        public RmvLodHeader CreateFromBase(RmvLodHeader source, uint lodLevel)
        {
            return new Rmv2LodHeader_V6()
            {
                _meshCount = source.MeshCount,
                _totalLodVertexSize = source.TotalLodVertexSize,
                _totalLodIndexSize = source.TotalLodIndexSize,
                _firstMeshOffset = source.FirstMeshOffset,
                _lodCameraDistance = source.LodCameraDistance,
            };
        }

        public byte[] Save(RmvLodHeader rmvLodHeader)
        {
            return ByteHelper.GetBytes((Rmv2LodHeader_V6)rmvLodHeader);
        }
    }
    public struct Rmv2LodHeader_V6 : RmvLodHeader
    {
        public uint _meshCount;
        public uint _totalLodVertexSize;
        public uint _totalLodIndexSize;
        public uint _firstMeshOffset;
        public float _lodCameraDistance;

        public uint MeshCount { get => _meshCount; set => _meshCount = value; }
        public uint TotalLodVertexSize { get => _totalLodVertexSize; set => _totalLodVertexSize = value; }
        public uint TotalLodIndexSize { get => _totalLodIndexSize; set => _totalLodIndexSize = value; }
        public uint FirstMeshOffset { get => _firstMeshOffset; set => _firstMeshOffset = value; }

        public byte QualityLvl { get => 0; }
        public float LodCameraDistance { get => _lodCameraDistance; set => _lodCameraDistance = value; }

        public int GetHeaderSize() => ByteHelper.GetSize(typeof(Rmv2LodHeader_V6));

        public RmvLodHeader Clone()
        {
            return new Rmv2LodHeader_V6()
            {
                _meshCount = MeshCount,
                _totalLodVertexSize = _totalLodVertexSize,
                _totalLodIndexSize = _totalLodIndexSize,
                _firstMeshOffset = _firstMeshOffset,
                _lodCameraDistance = _lodCameraDistance,
            };
        }

        public static Rmv2LodHeader_V6 CreateFromBase(RmvLodHeader header, uint lodLevel)
        {
            return new Rmv2LodHeader_V6()
            {
                _meshCount = header.MeshCount,
                _totalLodVertexSize = header.TotalLodVertexSize,
                _totalLodIndexSize = header.TotalLodIndexSize,
                _firstMeshOffset = header.FirstMeshOffset,
                _lodCameraDistance = header.LodCameraDistance,
            };
        }
    }


}
