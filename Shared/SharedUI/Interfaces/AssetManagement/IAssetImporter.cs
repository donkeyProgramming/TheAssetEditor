// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.Core.PackFiles.Models;

namespace Shared.Ui.Interfaces.AssetManagement
{
    public interface IAssetImporter
    {
        PackFile ImportAsset(string diskFilePath, AssetConfigData config = null);
        string[] Formats { get; }
    }
}
