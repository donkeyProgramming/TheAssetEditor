// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.WsModel;

namespace CommonControls.Interfaces.AssetManagement
{
    /// <summary>
    /// All the data an import/export _might_ need
    /// </summary>
    public class AssetParamData
    {
        public string DestinationPath { get; set; }
        public string SourcePath { get; set; }
        public PackFile InputPackFile { get; set; }
        public RmvFile RigidModelFile { set; get; }
        public WsMaterial wsmodelFile { set; get; }
        public AnimationFile skeletonFile { set; get; }
        public AnimationFile animationFile { set; get; }

        // TODO: maybe more?
    }

}
