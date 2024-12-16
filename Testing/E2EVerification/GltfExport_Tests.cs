namespace E2EVerification
{
    public class GltfExport_Tests
    {
        //private readonly string _normalFilePath01 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_body_01_normal.dds";
        //private readonly string _materialFilePath01 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_body_01_material_map.dds";
        //private readonly string _normalFilePath02 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_head_01_normal.dds";
        //private readonly string _materialFilePath02 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_head_01_material_map.dds";
        //private readonly string _packFile01 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";
        //private readonly string _packFile02 = @"variantmeshes\wh_variantmodels\hu1\emp\emp_props\emp_karl_franz_hammer_2h_01.rigid_model_v2";
        //
        //
        //
        //[Test]
        //public void convertNormal()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    runner.DdsToNormalPngExporterRepos.Export(_normalFilePath01, "C:/franz/convertNormal", true);
        //    var foundFile = ("C:/franz/convertNormal/" + "emp_karl_franz_body_01_normal.png");
        //    Assert.That(foundFile, Is.Not.Null);
        //}
        //[Test]
        //public void convertMaterial()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    runner.DdsToMaterialPngExporterRepos.Export(_materialFilePath01, "C:/franz/convertMaterial", true);
        //    var foundFile = ("C:/franz/convertMaterial/" + "emp_karl_franz_body_01_material_map.png");
        //    Assert.That(foundFile, Is.Not.Null);
        //}
        //[Test]
        //public void doNotConvertNormal()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    runner.DdsToNormalPngExporterRepos.Export(_normalFilePath02, "C:/franz/doNotConvertNormal", false);
        //    var foundFile = ("C:/franz/doNotConvertNormal/" + "emp_karl_franz_head_01_normal.png");
        //    Assert.That(foundFile, Is.Not.Null);
        //}
        //[Test]
        //public void doNotConvertMaterial()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    runner.DdsToMaterialPngExporterRepos.Export(_materialFilePath02, "C:/franz/doNotConvertMaterial", false);
        //    var foundFile = ("C:/franz/doNotConvertMaterial/" + "emp_karl_franz_head_01_material_map.png");
        //    Assert.That(foundFile, Is.Not.Null);
        //}
        //[Test]
        //public void rigidModelExportMaterial()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    var modelFile = runner.PackFileService.FindFile(_packFile01);
        //    runner.DdsToPngExporterRepos.Export("C:/franz/rigidExportConvert", modelFile, new RmvToGltfExporterSettings(runner.PackFileService.FindFile(_packFile01), "C:/franz/rigidExportConvert", true, true, true, true));
        //    var foundFile1 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_body_01_base_colour.png");
        //    var foundFile2 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_body_01_material_map.png");
        //    var foundFile3 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_body_01_normal.png");
        //    var foundFile4 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_head_01_base_colour.png");
        //    var foundFile5 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_head_01_material_map.png");
        //    var foundFile6 = ("C:/franz/rigidExportConvert/" + "emp_karl_franz_head_01_normal.png");
        //    Assert.That(foundFile1, Is.Not.Null);
        //    Assert.That(foundFile2, Is.Not.Null);
        //    Assert.That(foundFile3, Is.Not.Null);
        //    Assert.That(foundFile4, Is.Not.Null);
        //    Assert.That(foundFile5, Is.Not.Null);
        //    Assert.That(foundFile6, Is.Not.Null);
        //}
        //[Test]
        //public void rigidModelExportMaterialNoConvert()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    var modelFile = runner.PackFileService.FindFile(_packFile01);
        //    runner.DdsToPngExporterRepos.Export("C:/franz/rigidExportDoNotConvert", modelFile, new RmvToGltfExporterSettings(runner.PackFileService.FindFile(_packFile01), "C:/franz/rigidExportDoNotConvert", true, false, false, true));
        //    var foundFile1 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_body_01_base_colour.png");
        //    var foundFile2 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_body_01_material_map.png");
        //    var foundFile3 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_body_01_normal.png");
        //    var foundFile4 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_head_01_base_colour.png");
        //    var foundFile5 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_head_01_material_map.png");
        //    var foundFile6 = ("C:/franz/rigidExportDoNotConvert/" + "emp_karl_franz_head_01_normal.png");
        //    Assert.That(foundFile1, Is.Not.Null);
        //    Assert.That(foundFile2, Is.Not.Null);
        //    Assert.That(foundFile3, Is.Not.Null);
        //    Assert.That(foundFile4, Is.Not.Null);
        //    Assert.That(foundFile5, Is.Not.Null);
        //    Assert.That(foundFile6, Is.Not.Null);
        //}
        //[Test]
        //public void staticMeshExportMaterialConvert()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    var modelFile = runner.PackFileService.FindFile(_packFile02);
        //    runner.DdsToPngExporterRepos.Export("C:/franz/staticMeshConvert", modelFile, new RmvToGltfExporterSettings(runner.PackFileService.FindFile(_packFile01), "C:/franz/staticMeshConvert", true, true, true, true));
        //    var foundFile1 = ("C:/franz/staticMeshConvert/" + "emp_karl_franz_hammer_2h_01_base_colour.png");
        //    var foundFile2 = ("C:/franz/staticMeshConvert/" + "emp_karl_franz_hammer_2h_01_material_map.png");
        //    var foundFile3 = ("C:/franz/staticMeshConvert/" + "emp_karl_franz_hammer_2h_01_normal.png");
        //    Assert.That(foundFile1, Is.Not.Null);
        //    Assert.That(foundFile2, Is.Not.Null);
        //    Assert.That(foundFile3, Is.Not.Null);
        //}
        //[Test]
        //public void staticMeshExportMaterialDoNotConvert()
        //{
        //    var runner = new AssetEditorTestRunner();
        //    var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\normal_test.pack";
        //    var PackFile = runner.LoadPackFile(path);
        //    var modelFile = runner.PackFileService.FindFile(_packFile02);
        //    runner.DdsToPngExporterRepos.Export("C:/franz/staticMeshDoNotConvert", modelFile, new RmvToGltfExporterSettings(runner.PackFileService.FindFile(_packFile01), "C:/franz/staticMeshDoNotConvert", true, false, false, true));
        //    var foundFile1 = ("C:/franz/staticMeshDoNotConvert/" + "emp_karl_franz_hammer_2h_01_base_colour.png");
        //    var foundFile2 = ("C:/franz/staticMeshDoNotConvert/" + "emp_karl_franz_hammer_2h_01_material_map.png");
        //    var foundFile3 = ("C:/franz/staticMeshDoNotConvert/" + "emp_karl_franz_hammer_2h_01_normal.png");
        //    Assert.That(foundFile1, Is.Not.Null);
        //    Assert.That(foundFile2, Is.Not.Null);
        //    Assert.That(foundFile3, Is.Not.Null);
        //}

        //keeping these for future use as the pathways are inside the test pack already
        /**private readonly string _rmvFilePathCap = @"variantmeshes\wh_variantmodels\hu1\emp\emp_captains\body\emp_captains_body_01.rigid_model_v2";
        private readonly string _rmvFilePathArc = @"variantmeshes\wh_variantmodels\hu1\emp\emp_props\emp_arch_lector_hammer_1h_01.rigid_model_v2";
        private readonly string _rmvFilePathSta = @"variantmeshes\wh_variantmodels\hu1\tmb\tmb_props\tmb_arkhan_staff_1h_01.rigid_model_v2";
        private readonly string _rmvFilePathBelBod = @"variantmeshes\wh_variantmodels\hu10\dae\dae_belekor\dae_belakor_body_01.rigid_model_v2";
        private readonly string _rmvFilePathBelSwo = @"variantmeshes\wh_variantmodels\hu10\dae\dae_props\dae_belakor_sword_1h_01.rigid_model_v2";
        private readonly string _rmvFilePathKai = @"variantmeshes\wh_variantmodels\hu10f\dae\dae_kairos\dae_kairos_01.rigid_model_v2";
        private readonly string _rmvFilePathCyg = @"variantmeshes\wh_variantmodels\hu11\bst\bst_cygor\bst_cygor_01.rigid_model_v2";
        private readonly string _rmvFilePathCrone = @"variantmeshes\wh_variantmodels\hu1b\def\def_crone_hellebron\def_crone_hellebron_body_01.rigid_model_v2";
        private readonly string _rmvFilePathEpi = @"variantmeshes\wh_variantmodels\hu4d\dae\dae_epidemius\dae_epidemius_01.rigid_model_v2";
        private readonly string _rmvFilePathHu12 = @"variantmeshes\wh_variantmodels\hu12\dae\dae_khornataur\dae_khornataur_body_01.rigid_model_v2";
        private readonly string _rmvFilePathHu13 = @"variantmeshes\wh_variantmodels\hu13\cst\cst_bloated_corpse\cst_bloated_corpse_01.rigid_model_v2";
        private readonly string _rmvFilePathHu14 = @"variantmeshes\wh_variantmodels\hu14\dae\dae_horror_pink_exalted\dae_horror_pink_exalted_01.rigid_model_v2";
        private readonly string _rmvFilePathHu16 = @"variantmeshes\wh_variantmodels\hu16\dae\dae_daemonette\dae_daemonette_body_01.rigid_model_v2";
        private readonly string _rmvFilePathHu17 = @"variantmeshes\wh_variantmodels\hu17\skv\skv_stormvermin\torso\skv_stormvermin_torso_01.rigid_model_v2";
        private readonly string _rmvFilePathHu18 = @"variantmeshes\wh_variantmodels\hu18\skv\skv_rat_ogre_mutant\skv_rat_ogre_mutant_body_01.rigid_model_v2";
        private readonly string _rmvFilePathHu20 = @"variantmeshes\wh_variantmodels\hu20\wef\wef_drycha\wef_drycha_body_01.rigid_model_v2";

        [Test]
        public void humanoid01() //this will export the captain body with no textures (dummy one color textures), also a skeletal mesh on humanoid01
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var captainBody = runner.PackFileService.FindFile(_rmvFilePathCap);
            var settings = new RmvToGltfExporterSettings(captainBody, "C:/franz/", false, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(captainBody.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void prop01() //export textures with model but will not convert the material map, a static mesh prop
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var captainWeapon = runner.PackFileService.FindFile(_rmvFilePathArc);
            var settings = new RmvToGltfExporterSettings(captainWeapon, "C:/franz/", true, false, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(captainWeapon.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void prop02() //export textures with model but will not convert the normal map, a static mesh prop
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var staff = runner.PackFileService.FindFile(_rmvFilePathSta);
            var settings = new RmvToGltfExporterSettings(staff, "C:/franz/", true, true, false, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(staff.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid10() //export textures with model but will not convert the material map or the normal map, skeletal mesh on humanoid10
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var belBod = runner.PackFileService.FindFile(_rmvFilePathBelBod);
            var settings = new RmvToGltfExporterSettings(belBod, "C:/franz/", true, false, false, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(belBod.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void prop03() //exports model and all textures as well as converting the textures, a static mesh prop
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var belSwo = runner.PackFileService.FindFile(_rmvFilePathBelSwo);
            var settings = new RmvToGltfExporterSettings(belSwo, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(belSwo.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid10f() //exports model and all textures as well as converting the textures, the rest of the tests are to check various models and skeletons to make sure it is all proper
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var kairos = runner.PackFileService.FindFile(_rmvFilePathKai);
            var settings = new RmvToGltfExporterSettings(kairos, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(kairos.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid11()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var cygor = runner.PackFileService.FindFile(_rmvFilePathCyg);
            var settings = new RmvToGltfExporterSettings(cygor, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(cygor.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid01b()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var crone = runner.PackFileService.FindFile(_rmvFilePathCrone);
            var settings = new RmvToGltfExporterSettings(crone, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(crone.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid04d()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var epidemius = runner.PackFileService.FindFile(_rmvFilePathEpi);
            var settings = new RmvToGltfExporterSettings(epidemius, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(epidemius.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid12()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var khornataur = runner.PackFileService.FindFile(_rmvFilePathHu12);
            var settings = new RmvToGltfExporterSettings(khornataur, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(khornataur.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid13()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var bloatedCorpse = runner.PackFileService.FindFile(_rmvFilePathHu13);
            var settings = new RmvToGltfExporterSettings(bloatedCorpse, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(bloatedCorpse.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid14()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var horrorPinkExalt = runner.PackFileService.FindFile(_rmvFilePathHu14);
            var settings = new RmvToGltfExporterSettings(horrorPinkExalt, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(horrorPinkExalt.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid16()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var daemonetteBody = runner.PackFileService.FindFile(_rmvFilePathHu16);
            var settings = new RmvToGltfExporterSettings(daemonetteBody, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(daemonetteBody.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid17()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var stormvermin = runner.PackFileService.FindFile(_rmvFilePathHu17);
            var settings = new RmvToGltfExporterSettings(stormvermin, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(stormvermin.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid18()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var ratOgreMutant = runner.PackFileService.FindFile(_rmvFilePathHu18);
            var settings = new RmvToGltfExporterSettings(ratOgreMutant, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(ratOgreMutant.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        public void humanoid20()
        {
            var runner = new AssetEditorTestRunner();
            var path = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\test_export.pack";
            var PackFile = runner.LoadPackFile(path, true);
            var drycha = runner.PackFileService.FindFile(_rmvFilePathHu20);
            var settings = new RmvToGltfExporterSettings(drycha, "C:/franz/", true, true, true, true);
            runner.RmvToGltfExporterRepos.Export(settings);
            var foundFile = ("C:/franz/" + Path.GetFileNameWithoutExtension(drycha.Name) + ".gltf");
            Assert.That(foundFile, Is.Not.Null);
        }**/
    }
}
