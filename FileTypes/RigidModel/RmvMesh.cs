using Filetypes.RigidModel.Vertex;
using Microsoft.Xna.Framework;
using SharpDX;
using System;
using System.IO;

namespace Filetypes.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

