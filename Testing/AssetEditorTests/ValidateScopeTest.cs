using AssetEditor.Services;

namespace AssetEditorTests
{
    [TestClass]
    public class ValidateScopeTest
    {
        [TestMethod]
        public void Validate()
        {
            var cfg = new DependencyInjectionConfig(false);
            cfg.Build(true);
        }
    }
}
