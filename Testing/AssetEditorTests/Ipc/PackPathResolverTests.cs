using AssetEditor.Services.Ipc;

namespace AssetEditorTests.Ipc
{
    [TestClass]
    public class PackPathResolverTests
    {
        [TestMethod]
        public void ResolvePackPath_ExtractsVariantMeshesSuffix_FromAbsolutePath()
        {
            var input = @"C:\games\wh3\data\variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2";

            var result = PackPathResolver.ResolvePackPath(input);

            Assert.AreEqual(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2", result);
        }

        [TestMethod]
        public void ResolvePackPath_NormalizesForwardSlashes_AndQuotes()
        {
            var input = "\"variantmeshes/wh_variantmodels/bi1/cth/bird.rigid_model_v2\"";

            var result = PackPathResolver.ResolvePackPath(input);

            Assert.AreEqual(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2", result);
        }

        [TestMethod]
        public void ResolvePackPath_CollapsesRepeatedBackslashes()
        {
            var input = @"variantmeshes\\wh_variantmodels\\bi1\\cth\\bird.rigid_model_v2";

            var result = PackPathResolver.ResolvePackPath(input);

            Assert.AreEqual(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2", result);
        }

        [TestMethod]
        public void ResolvePackPath_ReturnsInput_WhenNoKnownRootFound()
        {
            var input = @"custom_folder\mesh.rigid_model_v2";

            var result = PackPathResolver.ResolvePackPath(input);

            Assert.AreEqual(input, result);
        }
    }
}
