using Editors.Ipc;
using Shared.Core.PackFiles.Models;

namespace Test.Ipc
{
   
    public class ExternalFileOpenExecutorTests
    {
        [Test]
        public void ShouldForceKitbash_ReturnsTrue_ForWsmodel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant_1.wsmodel", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldForceKitbash_ReturnsTrue_ForVariantMeshDefinition()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.variantmeshdefinition", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldForceKitbash_ReturnsFalse_ForRigidModel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.rigid_model_v2", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanImportIntoKitbash_ReturnsTrue_ForRigidModel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.rigid_model_v2", []);

            var result = ExternalFileOpenExecutor.CanImportIntoKitbash(file);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanImportIntoKitbash_ReturnsFalse_ForUnsupportedFile()
        {
            var file = PackFile.CreateFromBytes("something.anim", []);

            var result = ExternalFileOpenExecutor.CanImportIntoKitbash(file);

            Assert.That(result, Is.False);
        }
    }
}
