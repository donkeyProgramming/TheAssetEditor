// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.RigidModel
{
    public class RmvModel
    {
        public RmvCommonHeader CommonHeader { get; set; }
        public IMaterial Material { get; set; }
        public RmvMesh Mesh { get; set; }

        public RmvModel()
        { }

        public void UpdateModelTypeFlag(ModelMaterialEnum newValue)
        {
            var header = CommonHeader;
            header.ModelTypeFlag = newValue;
            CommonHeader = header;
        }

        public void UpdateBoundingBox(BoundingBox bb)
        {
            var header = CommonHeader;
            header.BoundingBox.UpdateBoundingBox(bb);
            CommonHeader = header;
        }
    }
}
