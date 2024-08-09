using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.Materials.Shaders.SpecGloss;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace E2EVerification
{
    internal class MaterialTestsRome
    {
        ResourceLibrary _resourceLib;
        PackFileService _pfs;
        ApplicationSettingsService _appSettings;
        PackFileContainer _outputPackfile;

        [SetUp]
        public void Setup()
        {
            _appSettings = new ApplicationSettingsService(GameTypeEnum.Rome_2);
            _pfs = new PackFileService(new PackFileDataBase(false), _appSettings, new GameInformationFactory(), null, null, null);
            _resourceLib = new ResourceLibrary(_pfs);
            _pfs.LoadFolderContainer(@"C:/Users/ole_k/source/repos/TheAssetEditor/Data/Rome_Man_And_Shield_Pack");

            _outputPackfile = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
        }

        [Test]
        public void CreateMaterial_Default()
        {
            var meshPackFile = _pfs.FindFile(@"variantmeshes/_variantmodels/man/helmets/carthaginian_pylos.rigid_model_v2");
            var rmv2 = ModelFactory.Create().Load(meshPackFile!.DataSource.ReadData());

            var abstractMaterialFactory = new CapabilityMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.Create(rmv2.ModelList[0][0], null);

            Assert.That(material, Is.TypeOf<DefaultMaterial>());

            //var defaultCapabiliy = material.TryGetCapability<DefaultCapability>();
            //Assert.That(defaultCapabiliy, Is.Not.Null);
            //Assert.That(defaultCapabiliy.MaterialMap.TexturePath, Is.EqualTo(@"variantmeshes/wh_variantmodels/hu1/emp/emp_props/tex/emp_karl_franz_hammer_2h_01_material_map.dds"));
            //Assert.That(defaultCapabiliy.MaterialMap.UseTexture, Is.True);
            //Assert.That(defaultCapabiliy.UseAlpha, Is.False);
        }

        [Test]
        public void SaveMaterial_Default()
        {
            var meshPackFile = _pfs.FindFile(@"variantmeshes/_variantmodels/man/helmets/carthaginian_pylos.rigid_model_v2");
            var rmv2 = ModelFactory.Create().Load(meshPackFile!.DataSource.ReadData());

            var abstractMaterialFactory = new CapabilityMaterialFactory(_appSettings, _pfs, _resourceLib);
            var material = abstractMaterialFactory.CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);


            MaterialToRmvSerializer s = new MaterialToRmvSerializer();
            s.CreateMaterialFromCapabilityMaterial(rmv2.ModelList[0][0].Material, material);
        }
    }
}
