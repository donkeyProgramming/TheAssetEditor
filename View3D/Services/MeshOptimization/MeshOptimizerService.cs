using View3D.Rendering;
using View3D.Rendering.Geometry;
using Simplygon;
using System;

namespace View3D.Services.MeshOptimization
{
    public class MeshOptimizerService 
    {
        public static MeshObject CreatedReducedCopy(MeshObject originalMesh, float factor)
        {
            MeshObject newMesh = originalMesh.Clone(false);
            using (ISimplygon sg = Loader.InitSimplygon(out var simplygonErrorCode, out var simplygonErrorMessage))
            {
                if (simplygonErrorCode == EErrorCodes.NoError)
                {
                    return SimplygonMeshOptimizer.GetReducedMeshCopy(sg, originalMesh, factor);
                }
                else // simplygon failed to initialize, use old mesh reducer (MeshDecimator)
                {
                    return DecimatorMeshOptimizer.GetReducedMeshCopy(originalMesh, factor);
                }
            }
        }
    }

}
