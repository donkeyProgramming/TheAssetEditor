// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Services;

namespace CommonControls.Interfaces.AssetManagement
{
    /// <summary>
    /// Converts asset data to another format
    /// </summary>
    public interface IAssetManager
    {
        AssetManagerData ManageAsset(AssetManagerData inputData);
        string[] Formats { get; } // formats supported by the implemention        
    }

    public interface IAssetManager1
    {   /// <summary>
        /// This is base class for any conversion/import/export
        /// ManageAsset(PNG) -> Binary ->
        /// </summary>
        /// <param name="packFileService">Data source</param>
        /// <returns>binary result</returns>        
        byte[] ExportAsset(string path, PackFileService packFileService);
        byte[] ImportAsset(string path, PackFileService packFileService);
        string[] Formats { get; } // formats supported by the implemention        
    }
}
