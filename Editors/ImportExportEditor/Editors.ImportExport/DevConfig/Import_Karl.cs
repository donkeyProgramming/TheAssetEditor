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
using Shared.Core.Settings;

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
            // For testing importing without going through the UI
            // 80% import/export debugging uses these
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
        }
    }
}
