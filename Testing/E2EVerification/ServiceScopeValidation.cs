using Shared.Core.Settings;
using Test.TestingUtility.Shared;

namespace Test.E2EVerification
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
