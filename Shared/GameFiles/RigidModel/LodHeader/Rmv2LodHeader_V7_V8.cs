using Microsoft.Extensions.Logging;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.RigidModel.LodHeader
{
    public class Rmv2LodHeader_V7_V8_Creator : ILodHeaderCreator
    {
        public uint HeaderSize => (uint)ByteHelper.GetSize(typeof(Rmv2LodHeader_V7_V8));

        public RmvLodHeader Create(byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V7_V8>(buffer, offset);
            return header;
        }


        public RmvLodHeader CreateFromBase(RmvLodHeader source, uint lodLevel)
        {
            var output = new Rmv2LodHeader_V7_V8()
            {
                _meshCount = source.MeshCount,
                _totalLodVertexSize = source.TotalLodVertexSize,
                _totalLodIndexSize = source.TotalLodIndexSize,
                _firstMeshOffset = source.FirstMeshOffset,
                _lodCameraDistance = source.LodCameraDistance,

                _lodLevel = lodLevel,
                _qualityLvl = 0,
                _padding0 = 125,
                _padding1 = 136,
                _padding2 = 174
            };

            if (source is Rmv2LodHeader_V7_V8 typedHeader)
            {
                output._qualityLvl = typedHeader._qualityLvl;
                output._padding0 = typedHeader._padding0;
                output._padding1 = typedHeader._padding1;
                output._padding2 = typedHeader._padding2;
            }

            return output;
        }

        public byte[] Save(RmvLodHeader rmvLodHeader)
        {
            return ByteHelper.GetBytes((Rmv2LodHeader_V7_V8)rmvLodHeader);
        }


        public RmvLodHeader CreateEmpty(float cameraDistance, uint lodLevel, byte qualityLevel)
        {
            var output = new Rmv2LodHeader_V7_V8()
            {
                _meshCount = 0,
                _totalLodVertexSize = 0,
                _totalLodIndexSize = 0,
                _firstMeshOffset = 0,
                _lodCameraDistance = cameraDistance,

                _lodLevel = lodLevel,
                _qualityLvl = qualityLevel,
                _padding0 = 125,
                _padding1 = 136,
                _padding2 = 174
            };
            return output;
        }
    }

    public struct Rmv2LodHeader_V7_V8 : RmvLodHeader
    {
        public uint _meshCount;
        public uint _totalLodVertexSize;
        public uint _totalLodIndexSize;
        public uint _firstMeshOffset;
        public float _lodCameraDistance;
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
    }
}
