// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
