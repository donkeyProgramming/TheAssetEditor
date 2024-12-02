using E2EVerification.Shared;
using Shared.Core.Settings;

namespace E2EVerification
{
    internal class ServiceScopeValidation
    {
        [Test]
        public void EnsureDependencyInjectionConfigIsValid()
        {
            var _ = new AssetEditorTestRunner(GameTypeEnum.Warhammer3, true);
        }
    }
}
