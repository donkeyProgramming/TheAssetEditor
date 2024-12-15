using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel;
using System.Linq.Expressions;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    /// <summary>
    /// Validates weighting Rmv2 meshes
    /// </summary>
    public class MeshWeightValidator
    {                       
        static public bool Validate(RmvMesh rmv2mesh)
        {
            foreach (var vertex in rmv2mesh.VertexList)
            {
                ValidateVertexWeighting(vertex);                
            }

            return true;
        }
        static public void ValidateVertexWeighting(CommonVertex v)
        {            
            if (v.WeightCount != v.BoneWeight.Length || v.WeightCount != v.BoneIndex.Length)
                throw new Exception("Error: Invalid Vertex Weight State");            

            if (v.WeightCount > 4)
                throw new Exception("Error: Vertex Weight Count > 4");

            var weightSum = v.BoneWeight.Sum();

            if (weightSum == float.NaN) 
                throw new Exception("NaN in bone weights");

            const float tolerance = 0.05f;
            if (weightSum < (1.0f - tolerance) || weightSum > (1.0f + tolerance))
                throw new Exception("Error: sum of weights not 1.0f");
                        
            if (v.WeightCount == 0)                          
                   throw new Exception("Error: Invalid weights, 1 of more null weights in weighted mesh");
        }
    }
}
