using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace AssetEditor.Services
{
    // When AE regains focus, typically after saving a pack in RPFM, reloads packs another program has
    // rewritten. Read-only packs reload straight away; an editable pack asks first, and once declined
    // isn't asked about again until it changes again. Until then its reads and saves are refused.
    public class PackChangedOnDiskChecker : IDisposable
    {
        private readonly IPackFileService _packFileService;
        private readonly PackReloader _packReloader;
        private readonly Dictionary<IPackFileContainer, PackFileStamp> _declined = [];
        private bool _isChecking;

        public PackChangedOnDiskChecker(IPackFileService packFileService, PackReloader packReloader)
        {
            _packFileService = packFileService;
            _packReloader = packReloader;

            if (Application.Current != null)
                Application.Current.Activated += OnApplicationActivated;
        }

        private void OnApplicationActivated(object? sender, EventArgs e) => CheckAll();

        public void CheckAll()
        {
            // The reload prompt itself deactivates and reactivates the window
            if (_isChecking)
                return;

            _isChecking = true;
            try
            {
                var loaded = _packFileService.GetAllPackfileContainers();
                foreach (var gone in _declined.Keys.Except(loaded).ToList())
                    _declined.Remove(gone);

                foreach (var container in loaded)
                    Check(container);
            }
            finally
            {
                _isChecking = false;
            }
        }

        private void Check(IPackFileContainer container)
        {
            if (container.IsCaPackFile || container.ContainerType != PackFileContainerType.Normal || string.IsNullOrWhiteSpace(container.SystemFilePath))
                return;

            if (_packFileService.HasChangedOnDisk(container) == false)
            {
                _declined.Remove(container);
                return;
            }

            // Deleted or moved: there is nothing to reload, and reads already explain the problem
            var stamp = PackFileStamp.Read(container.SystemFilePath);
            if (stamp == null)
                return;

            if (_declined.TryGetValue(container, out var declinedStamp) && declinedStamp == stamp)
                return;

            if (container.IsReadOnly)
            {
                _packReloader.Reload(container);
                return;
            }

            if (_packReloader.AskToReloadChangedPack(container))
                _declined.Remove(container);
            else
                _declined[container] = stamp;
        }

        public void Dispose()
        {
            if (Application.Current != null)
                Application.Current.Activated -= OnApplicationActivated;
        }
    }
}
