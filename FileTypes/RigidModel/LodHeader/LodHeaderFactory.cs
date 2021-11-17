using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.RigidModel.LodHeader
{
    public class LodHeaderFactory
    {
        Dictionary<RmvVersionEnum, ILodHeaderCreator> _lodHeaderCreators = new Dictionary<RmvVersionEnum, ILodHeaderCreator>();

        public static LodHeaderFactory Create() => new LodHeaderFactory();

        public LodHeaderFactory()
        {
            _lodHeaderCreators[RmvVersionEnum.RMV2_V6] = new Rmv2LodHeader_V6_Creator();
            _lodHeaderCreators[RmvVersionEnum.RMV2_V7] = new Rmv2LodHeader_V7_V8_Creator();
            _lodHeaderCreators[RmvVersionEnum.RMV2_V8] = new Rmv2LodHeader_V7_V8_Creator();
        }

        public RmvLodHeader[] LoadLodHeaders(byte[] data, int offset, RmvVersionEnum version, uint lodCount, out uint bytesRead)
        {
            var lodLoader = _lodHeaderCreators[version];

            var lodHeaders = new RmvLodHeader[lodCount];
            for (int i = 0; i < lodCount; i++)
                lodHeaders[i] = lodLoader.Create(data, ((int)lodLoader.HeaderSize * i) + offset);

            bytesRead = (uint)lodLoader.HeaderSize * lodCount;
            return lodHeaders;
        }

        public uint GetHeaderSize(RmvVersionEnum version)
        {
            return _lodHeaderCreators[version].HeaderSize;
        }

        internal byte[] Save(RmvVersionEnum version, RmvLodHeader rmvLodHeader)
        {
            return _lodHeaderCreators[version].Save(rmvLodHeader);
        }
    }
}
