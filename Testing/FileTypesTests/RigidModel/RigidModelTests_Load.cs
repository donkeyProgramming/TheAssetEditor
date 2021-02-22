using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileTypesTests.RigidModel
{
    class RigidModelTests_Load
    {

        PackFile GetMeshPackFile()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\variants_wp_.pack");

            var file = packFileService.FindFile(@"variantmeshes\wh_variantmodels\bc4\hef\hef_war_lion\hef_war_lion_02.rigid_model_v2");
            Assert.NotNull(file);
            return file as PackFile;
        }

        byte[] GetMeshData()
        {
            return GetMeshPackFile().DataSource.ReadData();
        }

        [Test]
        public void LoadWarLionTest()
        {
            RmvRigidModel model = new RmvRigidModel(GetMeshData(), "UnitTestModel");

            // Header
            Assert.AreEqual("RMV2", model.Header.FileType);
            Assert.AreEqual(4, model.Header.LodCount);
            Assert.AreEqual("bigcat04", model.Header.SkeletonName);
            Assert.AreEqual(7, model.Header.Version);

            // Lod headers
            Assert.AreEqual(4, model.LodHeaders.Length);

            Assert.AreEqual(3, model.LodHeaders[0].MeshCount);
            Assert.AreEqual(85530, model.LodHeaders[0].TotalLodIndexSize);
            Assert.AreEqual(357696, model.LodHeaders[0].TotalLodVertexSize);

            Assert.AreEqual(3, model.LodHeaders[1].MeshCount);
            Assert.AreEqual(55584, model.LodHeaders[1].TotalLodIndexSize);
            Assert.AreEqual(266848, model.LodHeaders[1].TotalLodVertexSize);

            Assert.AreEqual(2, model.LodHeaders[2].MeshCount);
            Assert.AreEqual(10890, model.LodHeaders[2].TotalLodIndexSize);
            Assert.AreEqual(71148, model.LodHeaders[2].TotalLodVertexSize);

            Assert.AreEqual(2, model.LodHeaders[3].MeshCount);
            Assert.AreEqual(6492, model.LodHeaders[3].TotalLodIndexSize);
            Assert.AreEqual(50316, model.LodHeaders[3].TotalLodVertexSize);
            
            // MeshList
        }

        [Test]
        public void SaveWarLion()
        {
            var originalMeshBytes = GetMeshData();
            RmvRigidModel model = new RmvRigidModel(originalMeshBytes, "UnitTestModel");
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                    model.SaveToByteArray(writer);

                var savedMeshBytes = ms.ToArray();
                Assert.AreEqual(originalMeshBytes.Length, savedMeshBytes.Length);

                for (int i = 0; i < originalMeshBytes.Length; i++)
                    Assert.AreEqual(originalMeshBytes[i], savedMeshBytes[i]);
            }
        }


        [Test]
        public void UpdateOffsets()
        {
            var meshData = GetMeshData();
            RmvRigidModel model = new RmvRigidModel(meshData, "UnitTestModel");

            model.UpdateOffsets();

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                    model.SaveToByteArray(writer);

                var bits = ms.ToArray();
                Assert.AreEqual(meshData.Length, bits.Length);

                for (int i = 0; i < meshData.Length; i++)
                    Assert.AreEqual(meshData[i], bits[i]);
            }
        }

    }
}
