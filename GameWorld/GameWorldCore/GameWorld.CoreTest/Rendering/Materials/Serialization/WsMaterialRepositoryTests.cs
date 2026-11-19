using GameWorld.Core.Rendering.Materials.Serialization;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.TestUtility;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Rendering.Materials.Serialization
{
    internal class WsMaterialRepositoryTests
    {
        [TestCase]
        public void AddMaterial_NotExising()
        {
            // Arrange 
            var pfs = new PackFileService(null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<name> customMaterialName </name>PostContent0";

            // Act
            var repo = new WsMaterialRepository(pfs);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath0));
            Assert.That(isNew, Is.True);
        }

        [TestCase]
        public void AddMaterial_ExistingButDifferent()
        {
            // Arrange 
            var pfs = new PackFileService(null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<name> customMaterialName </name>PostContent0";

            var materialPath1 = "content/material1.xml.material";
            var materialContent1 = "PreContent1<name> customMaterialName </name>PostContent0";

            // Act
            var repo = new WsMaterialRepository(pfs);
            repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var _);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent1, materialPath1, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath1));
            Assert.That(isNew, Is.True);
        }

        [TestCase]
        public void AddMaterial_ExistingAndEqualButDifferentName()
        {
            // Arrange 
            var pfs = new PackFileService( null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<name> customMaterialName </name>PostContent0";

            var materialPath1 = "content/material1.xml.material";
            var materialContent1 = "PreContent0<name> customMaterialName2 </name>PostContent0";

            // Act
            var repo = new WsMaterialRepository(pfs);
            repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var _);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent1, materialPath1, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath0));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void AddMaterial_ExistingAndEqualWithWhiteSpaceDiff()
        {
            // Arrange 
            var pfs = new PackFileService(null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<name> customMaterialName </name>PostContent0";

            var materialPath1 = "content/material1.xml.material";
            var materialContent1 = "  PreContent0  <name> customMaterialName2 </name>  PostContent0  ";

            // Act
            var repo = new WsMaterialRepository(pfs);
            repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var _);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent1, materialPath1, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath0));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void AddMaterial_ExistingAndEqualWithCapitalization()
        {
            // Arrange 
            var pfs = new PackFileService(null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<name> customMaterialName </name>PostContent0";

            var materialPath1 = "content/material1.xml.material";
            var materialContent1 = "PreContenT0<name> customMaterialName </name>PostContent0";

            // Act
            var repo = new WsMaterialRepository(pfs);
            repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var _);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent1, materialPath1, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath0));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void LoadExistingMaterials()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.CreateFromFolder(GameTypeEnum.Warhammer3, "Data\\Karl_and_celestialgeneral_Pack");

             // Act
             var repo = new WsMaterialRepository(pfs);
            var materialCount = repo.ExistingMaterialsCount();

            // Assert
            Assert.That(materialCount, Is.EqualTo(17));
        }

        [TestCase]
        public void LoadExistingMaterials_AddComplexEqual()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.CreateFromFolder(GameTypeEnum.Warhammer3, "Data\\Karl_and_celestialgeneral_Pack");

            var path = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\materials\emp_karl_franz_body_01_weighted2_alpha_off.xml.material";
            var content = PathHelper.GetFileContentAsString("Karl_and_celestialgeneral_Pack\\" + path);

            // Act
            var repo = new WsMaterialRepository(pfs);
            var newPath = repo.GetExistingOrAddMaterial(content, path, out var isNew);
            var materialCount = repo.ExistingMaterialsCount();

            // Assert
            Assert.That(materialCount, Is.EqualTo(17));
            Assert.That(newPath, Is.EqualTo(path));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void LoadExistingMaterials_AddDifferent()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.CreateFromFolder(GameTypeEnum.Warhammer3, "Data\\Karl_and_celestialgeneral_Pack");

            var path = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\materials\emp_karl_franz_body_01_weighted2_alpha_off.xml.material";
            var content = PathHelper.GetFileContentAsString("Karl_and_celestialgeneral_Pack\\" + path) + "content is no longer equal";

            // Act
            var repo = new WsMaterialRepository(pfs);
            var newPath = repo.GetExistingOrAddMaterial(content, path, out var isNew);
            var materialCount = repo.ExistingMaterialsCount();

            // Assert
            Assert.That(materialCount, Is.EqualTo(17)); // the old content no longer maps to the overwritten path
            Assert.That(newPath, Is.EqualTo(path));
            Assert.That(isNew, Is.True);
        }

        [TestCase]
        public void LoadExistingMaterials_AddDifferentOnlyInName()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.CreateFromFolder(GameTypeEnum.Warhammer3, "Data\\Karl_and_celestialgeneral_Pack");

            var path = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\materials\emp_karl_franz_body_01_weighted2_alpha_off.xml.material";
            var content = PathHelper.GetFileContentAsString("Karl_and_celestialgeneral_Pack\\" + path);
            var nameIndex = content.IndexOf("<name>");
            content = content.Insert(nameIndex + 6, "no_longer_the_same_");

            // Act
            var repo = new WsMaterialRepository(pfs);
            var newPath = repo.GetExistingOrAddMaterial(content, path, out var isNew);
            var materialCount = repo.ExistingMaterialsCount();

            // Assert
            Assert.That(materialCount, Is.EqualTo(17));
            Assert.That(newPath, Is.EqualTo(path));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void LoadExistingMaterials_SameContentInTwoPacks_UsesLastLoadedPack()
        {
            // Arrange
            var pfs = new PackFileService(null);
            var firstPack = pfs.CreateNewPackFileContainer("first", PackFileVersion.PFH5, PackFileCAType.RELEASE, false);
            var lastPack = pfs.CreateNewPackFileContainer("last", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            var content = "PreContent0<name> customMaterialName </name>PostContent0";
            pfs.AddFilesToPack(firstPack, [new NewPackFileEntry("first/materials", PackFile.CreateFromASCII("material0.xml.material", content))]);
            pfs.AddFilesToPack(lastPack, [new NewPackFileEntry("last/materials", PackFile.CreateFromASCII("material0.xml.material", content))]);

            // Act
            var repo = new WsMaterialRepository(pfs);
            var finalPath = repo.GetExistingOrAddMaterial(content, "content/material1.xml.material", out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(@"last\materials\material0.xml.material"));
            Assert.That(isNew, Is.False);
        }

        [TestCase]
        public void LoadExistingMaterials_PathOverriddenByLaterPack_OldContentNotOffered()
        {
            // Arrange
            var pfs = new PackFileService(null);
            var firstPack = pfs.CreateNewPackFileContainer("first", PackFileVersion.PFH5, PackFileCAType.RELEASE, false);
            var lastPack = pfs.CreateNewPackFileContainer("last", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            var shadowedContent = "PreContent0<name> customMaterialName </name>PostContent0";
            pfs.AddFilesToPack(firstPack, [new NewPackFileEntry("content", PackFile.CreateFromASCII("material0.xml.material", shadowedContent))]);
            pfs.AddFilesToPack(lastPack, [new NewPackFileEntry("content", PackFile.CreateFromASCII("material0.xml.material", "PreContent1<name> customMaterialName </name>PostContent1"))]);

            // Act
            var repo = new WsMaterialRepository(pfs);
            var finalPath = repo.GetExistingOrAddMaterial(shadowedContent, "content/material1.xml.material", out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo("content/material1.xml.material"));
            Assert.That(isNew, Is.True);
        }

        [TestCase]
        public void AddMaterial_NameMissingFromFile()
        {
            // Arrange 
            var pfs = new PackFileService(null);

            var materialPath0 = "content/material0.xml.material";
            var materialContent0 = "PreContent0<nothing> customMaterialName </name>PostContent0";

            // Act
            var repo = new WsMaterialRepository(pfs);
            var finalPath = repo.GetExistingOrAddMaterial(materialContent0, materialPath0, out var isNew);

            // Assert
            Assert.That(finalPath, Is.EqualTo(materialPath0));
            Assert.That(isNew, Is.True);
        }

    }
}
