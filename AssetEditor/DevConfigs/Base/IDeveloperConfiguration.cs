using Shared.Core.Services;
namespace Editors.Shared.DevConfig.Base;

public interface IDeveloperConfiguration
{
    void OpenFileOnLoad();
    void OverrideSettings(ApplicationSettings currentSettings);
}
