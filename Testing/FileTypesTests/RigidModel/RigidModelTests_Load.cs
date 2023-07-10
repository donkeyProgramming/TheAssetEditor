using NUnit.Framework;

namespace FileTypesTests.RigidModel
{
    class RigidModelTests_Load
    {
        /*
        PackFile GetWarLionModel()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\variants_wp_.pack");

            var file = packFileService.FindFile(@"variantmeshes\wh_variantmodels\bc4\hef\hef_war_lion\hef_war_lion_02.rigid_model_v2");
            Assert.NotNull(file);
            return file ;
        }

        PackFile GetWeaponModel()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\variants_wp_.pack");

            var file = packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_props\hef_ranger_sword_1h_03.rigid_model_v2");
            Assert.NotNull(file);
            return file ;
        }

        byte[] GetLionMeshData()
        {
            return GetWarLionModel().DataSource.ReadData();
        }

        byte[] GetWeaponMeshData()
        {
            return GetWeaponModel().DataSource.ReadData();
        }

        [Test]
        public void LoadWarLionTest()
        {
            RmvRigidModel model = new RmvRigidModel(GetLionMeshData(), "UnitTestModel");

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
            var originalMeshBytes = GetLionMeshData();
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
            var meshData = GetLionMeshData();
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
        }*/

        [Test]
        public void UpdateOffs2222et2s()
        {



            //var meshData = GetWeaponMeshData();
            //
            //Rmv2ModelNode node = new Rmv2ModelNode("NodeName");
            //node.SetModel(new RmvRigidModel(meshData, "UnitTestModelFileName"), null, null, new TestGeometryGraphicsContextFactory());
            //
            //// Edit the first vertex
            //var geometry = node.GetMeshNode(0, 0).Geometry;// as Rmv2Geometry;
            //geometry.TransformVertex(0, Matrix.CreateTranslation(new Vector3(10, 10, 10)));
            //
            //// Save
            //var bytes = node.Save(true);
            //var reloadedMesh = new RmvRigidModel(bytes, "UnitTestModelFileName");

            //model.UpdateOffsets();
            //
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    using (BinaryWriter writer = new BinaryWriter(ms))
            //        model.SaveToByteArray(writer);
            //
            //    var bits = ms.ToArray();
            //    Assert.AreEqual(meshData.Length, bits.Length);
            //
            //    for (int i = 0; i < meshData.Length; i++)
            //        Assert.AreEqual(meshData[i], bits[i]);
            //}
        }




    }
}
