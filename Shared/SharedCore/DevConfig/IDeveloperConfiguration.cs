using Shared.Core.Services;
namespace Shared.Core.DevConfig
{
    public interface IDeveloperConfiguration
    {
        void OpenFileOnLoad();
        void OverrideSettings(ApplicationSettings currentSettings);
    }
}
