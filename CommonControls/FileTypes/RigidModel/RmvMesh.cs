using CommonControls.FileTypes.RigidModel.Vertex;
using Microsoft.Xna.Framework;
using SharpDX;
using System;
using System.IO;

namespace CommonControls.FileTypes.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

