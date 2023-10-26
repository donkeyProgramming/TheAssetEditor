// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonControls.Interfaces.AssetManagement;

namespace AssetManagement.GeometryManagement
{
    public class AssetExporterProvider : IAssetExporterProvider
    {
        private readonly IEnumerable<IAssetExporter> _Exporters;

        public AssetExporterProvider(IEnumerable<IAssetExporter> Exporters)
        {
            _Exporters = Exporters;
        }

        public List<IAssetExporter> GetAllExporters() => _Exporters.ToList();

        public T GetExporter<T>() where T : IAssetExporter
        {
            throw new NotImplementedException();
        }

        public IAssetExporter GetExporter(string format)
        {
            var Exporter = _Exporters.Where(x => IsValid(format, x)).FirstOrDefault();
            if (Exporter == null)
                throw new Exception($"No Exporter found for {format}");
            return Exporter;
        }

        bool IsValid(string format, IAssetExporter Exporter)
        {
            var res = Exporter.Formats.Any(x => x.Equals(format, StringComparison.InvariantCultureIgnoreCase));
            return res;
        }
    }

}
