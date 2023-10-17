// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Interfaces.AssetManagement;

namespace AssetManagement.Strategies.Fbx.FbxAssetHandling
{
    public class AssetManagerFbxExport : IAssetManager
    {
        public AssetManagerData ManageAsset(AssetManagerData inputData)
        {
            // do the stuff
            return null;
        }

        public string[] Formats => new string[] { FileExtentions.RMVV2, FileExtentions.VWSMODEL, FileExtentions.VMD };

    }
}
