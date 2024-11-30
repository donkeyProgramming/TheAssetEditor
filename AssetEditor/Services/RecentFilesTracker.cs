using System;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Settings;

namespace AssetEditor.Services
{
    class RecentFilesTracker : IDisposable
    {
        private readonly IGlobalEventHub _globalEventHub;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public RecentFilesTracker(IGlobalEventHub globalEventHub, ApplicationSettingsService applicationSettingsService) 
        {
            _globalEventHub = globalEventHub;
            _applicationSettingsService = applicationSettingsService;
            _globalEventHub.Register<PackFileContainerAddedEvent>(this, Handler);
        }

        private void Handler(PackFileContainerAddedEvent e)
        {
            if (e.Container.IsCaPackFile)
                return;

            if (string.IsNullOrEmpty(e.Container.SystemFilePath))
                return;

            _applicationSettingsService.AddRecentlyOpenedPackFile(e.Container.SystemFilePath);
            _applicationSettingsService.Save();
        }

        public void Dispose()
        {
            _globalEventHub.UnRegister(this);
        }
    }
}
