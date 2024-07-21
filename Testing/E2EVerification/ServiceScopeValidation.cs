using E2EVerification.Shared;

namespace E2EVerification
{
    internal class ServiceScopeValidation
    {
        [Test]
        public void EnsureDependencyInjectionConfigIsValid()
        {
            var _ = new AssetEditorTestRunner(true);
        }
    }
}
