using Editors.Ipc;

namespace Test.Ipc
{
    public class PackPathResolverTests
    {
        [Test]
        public void ResolvePackPath_ExtractsVariantMeshesSuffix_FromAbsolutePath()
        {
            var input = @"C:\games\wh3\data\variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2";
            var result = PackPathResolver.ResolvePackPath(input);
            Assert.That(result, Is.EqualTo(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2"));
        }

        [Test]
        public void ResolvePackPath_NormalizesForwardSlashes_AndQuotes()
        {
            var input = "\"variantmeshes/wh_variantmodels/bi1/cth/bird.rigid_model_v2\"";
            var result = PackPathResolver.ResolvePackPath(input);
            Assert.That(result, Is.EqualTo(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2"));
        }

        [Test]
        public void ResolvePackPath_CollapsesRepeatedBackslashes()
        {
            var input = @"variantmeshes\\wh_variantmodels\\bi1\\cth\\bird.rigid_model_v2";
            var result = PackPathResolver.ResolvePackPath(input);
            Assert.That(result, Is.EqualTo(@"variantmeshes\wh_variantmodels\bi1\cth\bird.rigid_model_v2"));
        }

        [Test]
        public void ResolvePackPath_ReturnsInput_WhenNoKnownRootFound()
        {
            var input = @"custom_folder\mesh.rigid_model_v2";
            var result = PackPathResolver.ResolvePackPath(input);
            Assert.That(input, Is.EqualTo(result));
        }
    }
}
