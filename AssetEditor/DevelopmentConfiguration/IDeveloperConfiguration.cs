using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration;

public interface IDeveloperConfiguration
{
    void OpenFileOnLoad();
    void OverrideSettings(ApplicationSettings currentSettings);
    string[] MachineNames { get; }
    bool IsEnabled { get; }
}
