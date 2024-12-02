using System.IO;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats;
using SharpGLTF.Schema2;
using Shared.GameFormats.RigidModel.LodHeader;
using System.Windows.Forms.Design;

namespace Editors.ImportExport.DevConfig
{
    public class Import_Karl : IDeveloperConfiguration
    {
        private readonly IPackFileService _packFileService;
        private readonly GltfImporter _importer;

        public Import_Karl(IPackFileService packFileService, GltfImporter exporter)
        {
            _packFileService = packFileService;
            _importer = exporter;
        }

        public void OpenFileOnLoad()
        {
            const string inputGltfAe = @"C:\Users\Kaiza\Documents\Maharaja_GLTF_WeightCrap\PhazerMade\dae_prince_torso_base_01.gltf";
            const string inputGltfFileResaved = @"C:\Users\Kaiza\Documents\Maharaja_GLTF_WeightCrap\PhazerMade\dae_prince_test_BlendFile_convertoGLTF.gltf";

            var settingsAe = new GltfImporterSettings(inputGltfAe, true, null);
            var settingsResaved = new GltfImporterSettings(inputGltfFileResaved, true, null);

            _importer.Import(settingsResaved);

            //var modelRootAe = ModelRoot.Load(inputGltfAe);
            //var modelRootResaved = ModelRoot.Load(inputGltfFileResaved);

            //var rmv2FileAe = RmvMeshBuilder.Build(settingsAe, modelRootAe);
            //var rmv2FileResaved = RmvMeshBuilder.Build(settingsResaved, modelRootResaved);            
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
        }
    }
}
