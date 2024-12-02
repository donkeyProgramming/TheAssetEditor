using Shared.Core.Settings;
namespace Shared.Core.DevConfig
{
    public interface IDeveloperConfiguration
    {
        void OpenFileOnLoad();
        void OverrideSettings(ApplicationSettings currentSettings);
    }
}
