// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace CommonControls.Interfaces.AssetManagement
{
    public interface IAssetExporterProvider
    {
        List<IAssetExporter> GetAllExporters();
        T GetExporter<T>() where T : IAssetExporter;
        IAssetExporter GetExporter(string format);
    }

//public interface IAssetManagerProvider
//{
//    // ------ import ----
//    List<IAssetExporter> GetAllExporters();
//    IAssetExporter GetExporter(string format);

//    // ------ import ----
//    List<IAssetExporter> GetAllImporters();
//    IAssetExporter GetImporters(string format);
//}
}
