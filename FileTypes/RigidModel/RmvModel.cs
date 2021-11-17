using Filetypes.RigidModel.Transforms;
using FileTypes.RigidModel.MaterialHeaders;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Filetypes.RigidModel
{
    public class RmvModel
    {
        public RmvCommonHeader CommonHeader { get; set; }
        public IMaterial Material { get; set; }
        public RmvMesh Mesh { get; set; }

        public RmvModel()
        { }

       public RmvModel Clone()
       {       
           return new RmvModel()
           {
               CommonHeader = CommonHeader,
               Material = Material.Clone(),
               Mesh = null
           };
       }
    }

}
