using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;

namespace Test.KitbashEditor.LoadAndSave
{
    internal class LoadAndSave_RmvMaterials : LoadAndSaveBase
    {
        [Test]
        public void LoadAndSave_Rome2_StaticDecal()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshDecal);
            var material = GetMaterials(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(material.Main.UseAlpha, Is.EqualTo(false));
            Assert.That(material.Vert, Is.EqualTo(UiVertexFormat.Static));

            Assert.That(material.Adv.UseDirt, Is.EqualTo(false));
            Assert.That(material.Adv.UseDecal, Is.EqualTo(true));
            Assert.That(material.Adv.UseSkin, Is.EqualTo(false));

            Assert.That(material.Adv.DirtMap.TexturePath, Is.Empty);
            Assert.That(material.Adv.DirtMask.TexturePath, Is.Empty);
            Assert.That(material.Adv.DecalMask.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.SkinMask.TexturePath, Is.Empty);

            Assert.That(material.Adv.UvScale, Is.EqualTo(new Vector2(1, 1)));
            Assert.That(material.Adv.TextureTransform, Is.Not.EqualTo(Vector4.Zero));


            // Saving
            var savedMaterial = SaveAndGetMaterial(handle.Runner);
            Assert.That(savedMaterial.BinaryVertexFormat, Is.EqualTo(VertexFormat.Static));
            Assert.That(savedMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.decal));

            AssertParameterList(savedMaterial.FloatParams, []);
            AssertParameterList(savedMaterial.IntParams, [0]);
            AssertParameterList(savedMaterial.Vec4Params, [new RmvVector4(0.44604316f, 0.51079136f, 0.94964033f, 0.97122306f)]);

            AssertTexture(savedMaterial, TextureType.Decal_dirtmap, false);
            AssertTexture(savedMaterial, TextureType.Decal_dirtmask, false);
            AssertTexture(savedMaterial, TextureType.Decal_mask, true);
            AssertTexture(savedMaterial, TextureType.Skin_mask, false);
        }

        [Test]
        public void LoadAndSave_Rome2_DecalDirtMap()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshDecalDirt);
            var material = GetMaterials(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(material.Main.UseAlpha, Is.EqualTo(false));
            Assert.That(material.Vert, Is.EqualTo(UiVertexFormat.Static));

            Assert.That(material.Adv.UseDirt, Is.EqualTo(true));
            Assert.That(material.Adv.UseDecal, Is.EqualTo(true));
            Assert.That(material.Adv.UseSkin, Is.EqualTo(false));

            Assert.That(material.Adv.DirtMap.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DirtMask.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DecalMask.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.SkinMask.TexturePath, Is.Empty);

            Assert.That(material.Adv.UvScale, Is.EqualTo(new Vector2(0.75f, 0.75f)));
            Assert.That(material.Adv.TextureTransform, Is.Not.EqualTo(Vector4.Zero));

            // -------------
            var savedMaterial = SaveAndGetMaterial(handle.Runner);
            Assert.That(savedMaterial.BinaryVertexFormat, Is.EqualTo(VertexFormat.Static));
            Assert.That(savedMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.decal_dirtmap));

            AssertParameterList(savedMaterial.FloatParams, [0.75f, 0.75f]);
            AssertParameterList(savedMaterial.IntParams, [0, 1, 1]);
            AssertParameterList(savedMaterial.Vec4Params, [new RmvVector4(0, 0, 1, 1)]);

            AssertTexture(savedMaterial, TextureType.Decal_dirtmap, true);
            AssertTexture(savedMaterial, TextureType.Decal_dirtmask, true);
            AssertTexture(savedMaterial, TextureType.Decal_mask, true);
            AssertTexture(savedMaterial, TextureType.Skin_mask, false);
        }


        [Test]
        public void LoadAndSave_Rome2_DirtMap()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshDirt);
            var material = GetMaterials(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(material.Main.UseAlpha, Is.EqualTo(false));
            Assert.That(material.Vert, Is.EqualTo(UiVertexFormat.Static));

            Assert.That(material.Adv.UseDirt, Is.EqualTo(true));
            Assert.That(material.Adv.UseDecal, Is.EqualTo(false));
            Assert.That(material.Adv.UseSkin, Is.EqualTo(false));

            Assert.That(material.Adv.DirtMap.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DirtMask.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DecalMask.TexturePath, Is.Empty);
            Assert.That(material.Adv.SkinMask.TexturePath, Is.Empty);

            Assert.That(material.Adv.UvScale, Is.EqualTo(new Vector2(3, 3)));
            Assert.That(material.Adv.TextureTransform, Is.EqualTo(Vector4.Zero));

            // -------------
            var savedMaterial = SaveAndGetMaterial(handle.Runner);
            Assert.That(savedMaterial.BinaryVertexFormat, Is.EqualTo(VertexFormat.Static));
            Assert.That(savedMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.dirtmap));

            AssertParameterList(savedMaterial.FloatParams, [3, 3]);
            AssertParameterList(savedMaterial.IntParams, [0, 1, 1]);
            AssertParameterList(savedMaterial.Vec4Params, []);

            AssertTexture(savedMaterial, TextureType.Decal_dirtmap, true);
            AssertTexture(savedMaterial, TextureType.Decal_dirtmask, true);
            AssertTexture(savedMaterial, TextureType.Decal_mask, false);
            AssertTexture(savedMaterial, TextureType.Skin_mask, false);


        }

        [Test]
        public void LoadAndSave_Rome2_Skin()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshSkin);
            var material = GetMaterials(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(material.Main.UseAlpha, Is.EqualTo(false));
            Assert.That(material.Vert, Is.EqualTo(UiVertexFormat.Cinematic));

            Assert.That(material.Adv.UseDirt, Is.EqualTo(false));
            Assert.That(material.Adv.UseDecal, Is.EqualTo(false));
            Assert.That(material.Adv.UseSkin, Is.EqualTo(true));

            Assert.That(material.Adv.DirtMap.TexturePath, Is.Empty);
            Assert.That(material.Adv.DirtMask.TexturePath, Is.Empty);
            Assert.That(material.Adv.DecalMask.TexturePath, Is.Empty);
            Assert.That(material.Adv.SkinMask.TexturePath, Is.Not.Empty);

            Assert.That(material.Adv.UvScale, Is.EqualTo(new Vector2(1, 1)));
            Assert.That(material.Adv.TextureTransform, Is.EqualTo(Vector4.Zero));
        }

        [Test]
        public void LoadAndSave_Rome2_SkinDirtMap()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshSkinDirt);
            var material = GetMaterials(handle.Editor, 1);

            // Asser that the data loaded correctly
            Assert.That(material.Main.UseAlpha, Is.EqualTo(false));
            Assert.That(material.Vert, Is.EqualTo(UiVertexFormat.Weighted));

            Assert.That(material.Adv.UseDirt, Is.EqualTo(true));
            Assert.That(material.Adv.UseDecal, Is.EqualTo(false));
            Assert.That(material.Adv.UseSkin, Is.EqualTo(true));

            Assert.That(material.Adv.DirtMap.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DirtMask.TexturePath, Is.Not.Empty);
            Assert.That(material.Adv.DecalMask.TexturePath, Is.Empty);
            Assert.That(material.Adv.SkinMask.TexturePath, Is.Not.Empty);

            Assert.That(material.Adv.UvScale, Is.EqualTo(new Vector2(4f, 4f)));
            Assert.That(material.Adv.TextureTransform, Is.EqualTo(Vector4.Zero));
        }


    }
}
