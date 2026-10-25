using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Test.Audio
{
    // Shared plumbing for the audio research probes: opens a shipped .pack through the app's own
    // container loader rather than re-reading the format by hand, so what the probes see is what
    // the editor sees. Only the UI-facing services are stubbed.
    internal static class VanillaBankReader
    {
        public static ServiceProvider CreateProvider(string gameDirectory)
        {
            var services = new ServiceCollection();
            new Shared.Core.DependencyInjectionContainer().Register(services);
            new Editors.Audio.DependencyInjectionContainer().Register(services);

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowWaitCursor()).Returns(new Mock<IWaitCursor>().Object);
            services.AddSingleton(dialogs.Object);
            services.AddSingleton<IFileSystemAccess, FileSystemAccess>();

            var provider = services.BuildServiceProvider();

            var settings = provider.GetRequiredService<ApplicationSettingsService>();
            settings.CurrentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            settings.CurrentSettings.GameDirectories.Clear();
            settings.CurrentSettings.GameDirectories.Add(
                new ApplicationSettings.GamePathPair(GameTypeEnum.Warhammer3, gameDirectory));

            // The probes often open a mod pack on its own. With this on, the service refuses it and
            // puts up a modal message box from inside the test run, and the probe sees an empty pack.
            provider.GetRequiredService<IPackFileService>().EnforceGameFilesMustBeLoaded = false;

            return provider;
        }

        public static IPackFileContainer OpenPack(ServiceProvider provider, string packPath, bool markAsCa = true)
        {
            var container = provider.GetRequiredService<IPackFileContainerLoader>()
                .CreateFromPackFile(PackFileContainerType.Normal, packPath, true);
            container.IsCaPackFile = markAsCa;
            provider.GetRequiredService<IPackFileService>().AddContainer(container);
            return container;
        }

        /// <summary>Every .bnk in the given pack, as (path, raw bytes).</summary>
        public static List<(string Path, byte[] Bytes)> ReadMusicBanks(string packPath)
        {
            var gameDirectory = Path.GetDirectoryName(Path.GetDirectoryName(packPath))!;
            using var provider = CreateProvider(gameDirectory);
            var container = OpenPack(provider, packPath);

            return container.GetAllFiles()
                .Where(x => x.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.Key, x.Value.DataSource.ReadData()))
                .ToList();
        }
    }
}
