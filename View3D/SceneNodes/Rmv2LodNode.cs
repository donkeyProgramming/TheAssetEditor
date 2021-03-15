using System;
using System.Collections.Generic;
using System.Linq;

namespace View3D.SceneNodes
{
    public class Rmv2LodNode : GroupNode
    {
        public Rmv2LodNode(string name, int lodIndex) : base(name) { LodValue = lodIndex; }
        public int LodValue { get; set; }

        internal List<Rmv2MeshNode> GetModels()
        {
            var output = new List<Rmv2MeshNode>();
            foreach (var child in Children)
            {
                if (child is Rmv2MeshNode meshNode)
                {
                    output.Add(meshNode);
                }
                else if (child is GroupNode)
                {
                    foreach (var groupChild in child.Children)
                    {
                        if (groupChild is Rmv2MeshNode meshNode2)
                        {
                            output.Add(meshNode2);
                        }
                    }
                }
            }

            return output;
        }
    }

 
}
