using Shared.Core.PackFiles;
using System.Linq;

namespace KitbasherEditor.Services
{
    public class MapLoaderService
    {
        public static void Load(PackFileService pfs, KitbashSceneCreator modelLoaderService)
        {
            //var allFiles = pfs.FindAllFilesInDirectory(@"terrain\campaigns\wh2_main_great_vortex_map_1\global_meshes");
            //var landMeshes = allFiles.Where(x => x.Extention == ".rigid_model_v2" && x.Name.Contains("land_mesh", StringComparison.InvariantCultureIgnoreCase));
            //var seaMeshes = allFiles.Where(x => x.Extention == ".rigid_model_v2" && x.Name.Contains("sea_mesh", StringComparison.InvariantCultureIgnoreCase));
            //
            //foreach(var item in landMeshes)
            //    modelLoaderService.LoadModelIntoMainScene(item);
            //
            //foreach (var item in seaMeshes)
            //    modelLoaderService.LoadModelIntoMainScene(item);


            var allTils = pfs.FindAllFilesInDirectory(@"terrain\tiles\campaign");
            var allTileMeshes = allTils.Where(x => x.Extention == ".rigid_model_v2");
            var failed = 0;

            foreach (var item in allTileMeshes)
            {
                try
                {
                    modelLoaderService.LoadModelIntoMainScene(item);
                }
                catch
                {
                    failed++;
                }
            }

            //modelLoaderService.LoadMainEditableModel();

            // terrain\campaigns\wh2_main_great_vortex_map_1\global_meshes\land_mesh_11.rigid_model_v2
            // terrain\campaigns\wh2_main_great_vortex_map_1\global_meshes\sea_mesh_9.rigid_model_v2

            // terrain\tiles\campaign\cliff_base\2x2_base\mesh.rigid_model_v2

        }
    }
}
