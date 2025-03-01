namespace Test.KitbashEditor.LoadAndSave
{
    internal class LoadAndSave_AttachmentPoints : LoadAndSaveBase
    {
        [Test]
        public void LoadAndSave_Rome2_CustomPoints()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshHelmet);
            var mainNode = GetMainNode(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(mainNode.AttachmentPoints.Count, Is.EqualTo(8));
 
            // Saving
            var savedMaterial = SaveAndGetMaterial(handle.Runner);
            Assert.That(savedMaterial.AttachmentPointParams.Count, Is.EqualTo(8));
        }

        [Test]
        public void LoadAndSave_Rome2_NoPoints()
        {
            var handle = CreateKitbashTool(TestFiles.RomePack_MeshDecalDirt);
            var mainNode = GetMainNode(handle.Editor);

            // Asser that the data loaded correctly
            Assert.That(mainNode.AttachmentPoints.Count, Is.EqualTo(0));

            // Saving
            var savedMaterial = SaveAndGetMaterial(handle.Runner);
            Assert.That(savedMaterial.AttachmentPointParams.Count, Is.EqualTo(0));
        }
    }
}
