using GameWorld.Core.Rendering.Shading.Factories;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.Core.Services.SceneSaving.Material.Strategies;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace E2EVerification
{
    internal class MaterialTests
    {
        ResourceLibrary _resourceLib;
        PackFileService _pfs;
        ApplicationSettingsService _appSettings;
        PackFileContainer _outputPackfile;

        [OneTimeSetUp]
        public void Setup()
        {
            _appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            _pfs = new PackFileService(new PackFileDataBase(false), _appSettings, new GameInformationFactory(), null, null, null);
            _resourceLib = new ResourceLibrary(_pfs);
            _pfs.LoadFolderContainer(@"C:/Users/ole_k/source/repos/TheAssetEditor/Data/Karl_and_celestialgeneral_Pack");

            _outputPackfile = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
        }

        [Test]
        public void CreateMaterial_Wh3_Default()
        {
            var meshPackFile = _pfs.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_props\emp_karl_franz_hammer_2h_01.rigid_model_v2");
            var rmv2 = ModelFactory.Create().Load(meshPackFile!.DataSource.ReadData());

            var abstractMaterialFactory = new AbstractMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.CreateFactory().Create(rmv2.ModelList[0][0], null);

            // Verify shader type and textures
        }

        //public void CreateMaterial_Wh3_Emissive()
        //public void CreateMaterial_Wh2_Default()
        //public void CreateMaterial_Rome2_Default()
        //public void CreateMaterial_Rome2_Decals()

        [Test]
        public void GenerateWsModel_MetalRoughPbr_Default()
        {
            var abstractMaterialFactory = new AbstractMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.CreateFactory().CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);

            var input = new WsModelGeneratorInput[3][]
            {
                [new WsModelGeneratorInput(0, 0, "Mesh", UiVertexFormat.Cinematic, material)],
                [new WsModelGeneratorInput(1, 0, "Mesh", UiVertexFormat.Weighted, material)],
                [new WsModelGeneratorInput(2, 0, "Mesh", UiVertexFormat.Static, material)]
            };

            var capabilityMaterialBuilder = CapabilityMaterialFactory.GetBuilder(_appSettings.CurrentSettings.CurrentGame);
            var wsModelGenerator = new WsModelGeneratorService(_pfs);
            wsModelGenerator.GenerateWsModel("Path", input, capabilityMaterialBuilder);

            var wsModel = _pfs.FindFile("WsModel");
            var wsMatrial = _pfs.FindFile("Material");
        }

      
    }
}
