using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.LodHeader
{
    public struct Rmv2LodHeader_V6 : RmvLodHeader
    {
        uint _meshCount;
        uint _totalLodVertexSize;
        uint _totalLodIndexSize;
        uint _firstMeshOffset;
        float _lodCameraDistance;

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
    }
}
