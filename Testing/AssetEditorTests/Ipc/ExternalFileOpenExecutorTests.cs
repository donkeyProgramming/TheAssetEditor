using AssetEditor.Services.Ipc;
using Shared.Core.PackFiles.Models;

namespace AssetEditorTests.Ipc
{
    [TestClass]
    public class ExternalFileOpenExecutorTests
    {
        [TestMethod]
        public void ShouldForceKitbash_ReturnsTrue_ForWsmodel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant_1.wsmodel", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldForceKitbash_ReturnsTrue_ForVariantMeshDefinition()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.variantmeshdefinition", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldForceKitbash_ReturnsFalse_ForRigidModel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.rigid_model_v2", []);

            var result = ExternalFileOpenExecutor.ShouldForceKitbash(file);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanImportIntoKitbash_ReturnsTrue_ForRigidModel()
        {
            var file = PackFile.CreateFromBytes("arb_base_elephant.rigid_model_v2", []);

            var result = ExternalFileOpenExecutor.CanImportIntoKitbash(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanImportIntoKitbash_ReturnsFalse_ForUnsupportedFile()
        {
            var file = PackFile.CreateFromBytes("something.anim", []);

            var result = ExternalFileOpenExecutor.CanImportIntoKitbash(file);

            Assert.IsFalse(result);
        }
    }
}
