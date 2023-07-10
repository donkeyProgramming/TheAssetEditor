// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.FileTypes.RigidModel.Vertex;

namespace CommonControls.FileTypes.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

