using Shared.Core.Services;
namespace AssetEditor.DevConfigs.Base;

public interface IDeveloperConfiguration
{
    void OpenFileOnLoad();
    void OverrideSettings(ApplicationSettings currentSettings);
}
