using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.Materials.Shaders.MetalRough;
using GameWorld.Core.Services.SceneSaving.Material;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace E2EVerification
{


    //public void CreateMaterial_Wh3_Emissive()
    //public void CreateMaterial_Wh2_Default()
    //public void CreateMaterial_Rome2_Default()
    //public void CreateMaterial_Rome2_Decals()
    internal class MaterialTestsWarhammer
    {
        ResourceLibrary _resourceLib;
        PackFileService _pfs;
        ApplicationSettingsService _appSettings;
        PackFileContainer _outputPackfile;

        [SetUp]
        public void Setup()
        {
            _appSettings = new ApplicationSettingsService();
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

            var abstractMaterialFactory = new CapabilityMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.Create(rmv2.ModelList[0][0], null);

            Assert.That(material, Is.TypeOf<DefaultMaterial>());

            var defaultCapabiliy = material.TryGetCapability<MetalRoughCapability>();
            Assert.That(defaultCapabiliy, Is.Not.Null);
            Assert.That(defaultCapabiliy.MaterialMap.TexturePath, Is.EqualTo(@"variantmeshes/wh_variantmodels/hu1/emp/emp_props/tex/emp_karl_franz_hammer_2h_01_material_map.dds"));
            Assert.That(defaultCapabiliy.MaterialMap.UseTexture, Is.True);
            Assert.That(defaultCapabiliy.UseAlpha, Is.False);
        }

        [Test]
        public void GenerateWsModel_Wh3_MetalRoughPbr_Default()
        {
            var abstractMaterialFactory = new CapabilityMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);

            Assert.That(material, Is.TypeOf<DefaultMaterial>());

            material.GetCapability<MetalRoughCapability>().NormalMap.TexturePath = "customFolder/customfilename.dds";

            List<WsModelGeneratorInput> input =  
            [
                new WsModelGeneratorInput(0, 0, "MesheshThatShareMaterial", UiVertexFormat.Cinematic, material),
                new WsModelGeneratorInput(0, 1, "MesheshThatShareMaterial", UiVertexFormat.Cinematic, material),
                new WsModelGeneratorInput(1, 0, "Mesh", UiVertexFormat.Weighted, material),
                new WsModelGeneratorInput(2, 0, "Mesh", UiVertexFormat.Static, material),
            ];

            var materialSerializerFactory = new MaterialToWsMaterialFactory(_pfs);
            var wsModelGenerator = new WsModelGeneratorService(_pfs);
            var result = wsModelGenerator.GenerateWsModel(materialSerializerFactory.CreateInstance(GameTypeEnum.Warhammer3), @"variantmeshes\wh_variantmodels\hu1\emp\emp_props\emp_karl_franz_hammer_2h_01.rigid_model_v2", input);

            Assert.That(result.Status, Is.True);
            Assert.That(result.CreatedFilePath, Is.EqualTo("variantmeshes\\wh_variantmodels\\hu1\\emp\\emp_props\\emp_karl_franz_hammer_2h_01.wsmodel"));

            var wsModelPackFile = _pfs.FindFile(result.CreatedFilePath, _outputPackfile);
            
            var wsModel = new WsModelFile(wsModelPackFile);
            var path0 = wsModel.MaterialList.First(x => x.LodIndex == 0 && x.PartIndex == 0).MaterialPath;
            var path1 = wsModel.MaterialList.First(x => x.LodIndex == 0 && x.PartIndex == 1).MaterialPath;
            var path2 = wsModel.MaterialList.First(x => x.LodIndex == 1 && x.PartIndex == 0).MaterialPath;
            var path3 = wsModel.MaterialList.First(x => x.LodIndex == 2 && x.PartIndex == 0).MaterialPath;

            Assert.That(path0, Is.EqualTo(path1));


            var wsMatrial0 = _pfs.FindFile(path0, _outputPackfile);
            var wsMatrial1 = _pfs.FindFile(path0, _outputPackfile);
            var wsMatrial2 = _pfs.FindFile(path0, _outputPackfile);
        }


        void VerifyMaterial(WsModelMaterialFile wsModelMaterialFile)
        { 
        
        
        
        }

    }
}
