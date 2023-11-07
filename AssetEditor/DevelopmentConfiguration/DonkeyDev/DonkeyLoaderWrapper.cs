using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev;

using CurrentType = KitbashEditor_KarlFranzeEditorDevelopmentConfiguration;

internal abstract class DonkeyConfigurationBase<T> : IDeveloperConfiguration
{
    public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
    public bool IsEnabled => typeof(T) == typeof(CurrentType);
    public virtual void OpenFileBeforeLoad() { }
    public virtual void OpenFileOnLoad() { }
    public virtual void OverrideSettings(ApplicationSettings currentSettings) { }
}
