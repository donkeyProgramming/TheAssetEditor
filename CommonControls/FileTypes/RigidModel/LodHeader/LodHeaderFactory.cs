// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace CommonControls.FileTypes.RigidModel.LodHeader
{
    public class LodHeaderFactory
    {
        Dictionary<RmvVersionEnum, ILodHeaderCreator> _lodHeaderCreators = new Dictionary<RmvVersionEnum, ILodHeaderCreator>();

        public static LodHeaderFactory Create() => new LodHeaderFactory();

        public LodHeaderFactory()
        {
            //_lodHeaderCreators[RmvVersionEnum.RMV2_V5] = new Rmv2LodHeader_V6_Creator();
            _lodHeaderCreators[RmvVersionEnum.RMV2_V6] = new Rmv2LodHeader_V6_Creator();
            _lodHeaderCreators[RmvVersionEnum.RMV2_V7] = new Rmv2LodHeader_V7_V8_Creator();
            _lodHeaderCreators[RmvVersionEnum.RMV2_V8] = new Rmv2LodHeader_V7_V8_Creator();
        }

        public RmvLodHeader[] LoadLodHeaders(byte[] data, int offset, RmvVersionEnum version, uint lodCount, out uint bytesRead)
        {
            if (_lodHeaderCreators.ContainsKey(version) == false)
                throw new Exception($"Unknown Lod header - {version}");
            var lodLoader = _lodHeaderCreators[version];

            var lodHeaders = new RmvLodHeader[lodCount];
            for (int i = 0; i < lodCount; i++)
                lodHeaders[i] = lodLoader.Create(data, (int)lodLoader.HeaderSize * i + offset);

            bytesRead = lodLoader.HeaderSize * lodCount;
            return lodHeaders;
        }

        public uint GetHeaderSize(RmvVersionEnum version)
        {
            return _lodHeaderCreators[version].HeaderSize;
        }

        public byte[] Save(RmvVersionEnum version, RmvLodHeader rmvLodHeader)
        {
            return _lodHeaderCreators[version].Save(rmvLodHeader);
        }

        public RmvLodHeader CreateFromBase(RmvVersionEnum version, RmvLodHeader source, uint lodLevel)
        {
            return _lodHeaderCreators[version].CreateFromBase(source, lodLevel);
        }
    }
}
