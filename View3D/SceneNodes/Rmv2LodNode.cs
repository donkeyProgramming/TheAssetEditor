using System;
using System.Collections.Generic;

namespace View3D.SceneNodes
{
    public class Rmv2LodNode : GroupNode
    {
        public Rmv2LodNode(string name, int lodIndex) : base(name) { LodValue = lodIndex; }
        public int LodValue { get; set; }

        internal List<Rmv2MeshNode> GetModels()
        {
            throw new NotImplementedException();
        }
    }

 
}
