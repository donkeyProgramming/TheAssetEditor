using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.LodHeader
{
    public struct Rmv2LodHeader_V7_V8 : RmvLodHeader
    {
        uint _meshCount;
        uint _totalLodVertexSize;
        uint _totalLodIndexSize;
        uint _firstMeshOffset;
        float _lodCameraDistance;
        public uint _lodLevel;
        public byte _qualityLvl;
        public byte _padding0;
        public byte _padding1;
        public byte _padding2;

        public uint MeshCount { get => _meshCount; set => _meshCount = value; }
        public uint TotalLodVertexSize { get => _totalLodVertexSize; set => _totalLodVertexSize = value; }
        public uint TotalLodIndexSize { get => _totalLodIndexSize; set => _totalLodIndexSize = value; }
        public uint FirstMeshOffset { get => _firstMeshOffset; set => _firstMeshOffset = value; }

        public byte QualityLvl { get => _qualityLvl; set => _qualityLvl = value; }
        public float LodCameraDistance { get => _lodCameraDistance; set => _lodCameraDistance = value; }

        public int GetHeaderSize() => ByteHelper.GetSize(typeof(Rmv2LodHeader_V7_V8));

        public RmvLodHeader Clone()
        {
            return new Rmv2LodHeader_V7_V8()
            {
                _meshCount = MeshCount,
                _totalLodVertexSize = _totalLodVertexSize,
                _totalLodIndexSize = _totalLodIndexSize,
                _firstMeshOffset = _firstMeshOffset,
                _lodCameraDistance = _lodCameraDistance,

                _lodLevel = _lodLevel,
                _qualityLvl = _qualityLvl,
                _padding0 = _padding0,
                _padding1 = _padding1,
                _padding2 = _padding2,
            };
        }

        public static Rmv2LodHeader_V7_V8 CreateFromBase(RmvLodHeader header, uint lodLevel)
        {
            var output = new Rmv2LodHeader_V7_V8()
            {
                _meshCount = header.MeshCount,
                _totalLodVertexSize = header.TotalLodVertexSize,
                _totalLodIndexSize = header.TotalLodIndexSize,
                _firstMeshOffset = header.FirstMeshOffset,
                _lodCameraDistance = header.LodCameraDistance,

                _lodLevel = lodLevel,
                _qualityLvl = 0,
                _padding0 = 125,
                _padding1 = 136,
                _padding2 = 174
            };

            if(header is Rmv2LodHeader_V7_V8 typedHeader)
            {
                output._lodLevel = typedHeader._qualityLvl;
                output._padding0 = typedHeader._padding0;
                output._padding1 = typedHeader._padding1;
                output._padding2 = typedHeader._padding2;
            }

            return output;
        }
    }
}
