// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonControls.Interfaces.AssetManagement;

namespace AssetManagment.AssetManagement
{
    public class FileExtentions
    {
        public const string RMVV2 = "rigid_mode_v2";
        public const string VWSMODEL = "wsmodel";
        public const string VMD = "wsmodel";
        public const string OBJ = "obj";
        public const string FBX = "FBX";
    }
   
    public class AssetManagerProvider : IAssetManagerProvider
    {
        private readonly IEnumerable<IAssetManager> _exporters;

        public AssetManagerProvider(IEnumerable<IAssetManager> importers)
        {
            _exporters = importers;
        }

        public List<IAssetManager> GetAllIAssetManagers() => _exporters.ToList();

        public T GetAssetManager<T>() where T : IAssetManager
        {
            throw new NotImplementedException();
        }

        public T GetAssetManagerDataType<T, INPUT_DATA>(INPUT_DATA inputData) where T : IAssetManager
        {
            throw new NotImplementedException();
        }

        public IAssetManager GetAssetManager(string format)
        {
            var Exporter = _exporters.Where(x => IsValid(format, x)).FirstOrDefault();
            if (Exporter == null)
                throw new Exception($"No Exporter found for {format}");
            return Exporter;
        }

        /// <summary>
        /// Converts FROM the "format" to generic data
        /// </summary>        
        public IAssetManager GetAssetImporter(string format)
        {
            var Exporter = _exporters.Where(x => IsValid(format, x)).FirstOrDefault();
            if (Exporter == null)
                throw new Exception($"No Exporter found for {format}");
            return Exporter;
        }

        bool IsValid(string Inputformat, IAssetManager importer)
        {
            var resInput = importer.Formats.Any(x => x.Equals(Inputformat, StringComparison.InvariantCultureIgnoreCase));
            return resInput;
        }
    }
};
