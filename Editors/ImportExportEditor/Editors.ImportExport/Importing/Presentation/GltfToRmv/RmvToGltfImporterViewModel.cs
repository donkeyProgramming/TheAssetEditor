using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Presentation.RmvToGltf;
using Editors.ImportExport.Importing.Importers;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Presentation;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.Common.DataTemplates;
using Editors.ImportExport.Importing.Presentation.RmvToGltf;
using Editors.ImportExport.Common;

namespace Editors.ImportImport.Importing.Presentation.RmvToGltf
{
    public partial class RmvToGltfImporterViewModel : ObservableObject, IImporterViewModel, IViewProvider<RmvToGltfImporterView>
    {
        private readonly GltfImporter _Importer;

        public string DisplayName => "Gltf Importer";
        public string OutputExtension => ".rigid_model_v2";
        public string[] InputExtensions => new string[] { ".gltf", ".glb" };

        [ObservableProperty] bool _importMeshes = true;        
        [ObservableProperty] bool _importMaterials = true;
        [ObservableProperty] bool _convertFromBlenderMaterialMap = true;
        [ObservableProperty] bool _convertNormalTextureToOrange = true;
        [ObservableProperty] bool _importAnimations = true;
        [ObservableProperty] float _animationKeysPerSecond = 20.0f;

        public RmvToGltfImporterViewModel(GltfImporter Importer)
        {
            _Importer = Importer;
        }

        public ImportExportSupportEnum CanImportFile(PackFile file) => _Importer.CanImportFile(file);

        public void Execute(PackFile ImportSource, string outputPath, PackFileContainer packFileContainer, GameTypeEnum gameType)
        {
            // TODO: fill in the rest of the parameters from the UI
            // TODO: test each param on and off
            var settings = new GltfImporterSettings(ImportSource.Name, outputPath, packFileContainer, gameType, true, true, true, true, true, 20.0f, true);

            _Importer.Import(settings);
        }
    }
}
