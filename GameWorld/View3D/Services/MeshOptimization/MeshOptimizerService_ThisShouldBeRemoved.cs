///using Simplygon;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace GameWorld.Core.Services.MeshOptimization
{
    public class MeshOptimizerService_ThisShouldBeRemoved
    {
        public static MeshObject CreatedReducedCopy(MeshObject originalMesh, float factor)
        {
            //MeshObject newMesh = originalMesh.Clone(false);
            //using (ISimplygon sg = Loader.InitSimplygon(out var simplygonErrorCode, out var simplygonErrorMessage))
            //{
            //    if (simplygonErrorCode == EErrorCodes.NoError)
            //    {
            //        return SimplygonMeshOptimizer.GetReducedMeshCopy(sg, originalMesh, factor);
            //    }
            //    else // simplygon failed to initialize, use old mesh reducer (MeshDecimator)
            //    {
            return DecimatorMeshOptimizer.GetReducedMeshCopy(originalMesh, factor);
            //    }
            //}
        }
    }

}
