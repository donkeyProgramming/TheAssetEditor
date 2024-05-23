using AssetEditor.Services;

namespace AssetEditorTests
{
    [TestClass]
    public class ValidateScopeTest
    {
        [TestMethod]
        public void Validate()
        {
            DependencyInjectionConfig cfg = new DependencyInjectionConfig(false);
            cfg.Build();
        }
    }
}