using Simplygon;
using View3D.Rendering.Geometry;
using View3D.Services.SceneSaving.Lod.MeshDecimatorIntegration;
using View3D.Services.SceneSaving.Lod.SimplygonIntegration;

namespace View3D.Services.MeshOptimization
{
    public class MeshOptimizerService_ThisShouldBeRemoved
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
