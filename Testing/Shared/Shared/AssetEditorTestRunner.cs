using AssetEditor.Services;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Services;
using GameWorld.Core.WpfWindow;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.StandardDialog;

namespace Test.TestingUtility.Shared
{
    public class AssetEditorTestRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public IServiceProvider ServiceProvider => _serviceProvider;
        public IPackFileService PackFileService { get; private set; }
        public IUiCommandFactory CommandFactory { get; private set; }
        public IScopeRepository ScopeRepository { get; private set; }
        public Mock<IStandardDialogs> Dialogs { get; private set; }


        public AssetEditorTestRunner(GameTypeEnum gameEnum = GameTypeEnum.Warhammer3, bool forceValidateServiceScopes = false)
        {
            _serviceProvider = new DependencyInjectionConfig(false).Build(forceValidateServiceScopes, MockServices);
  
            var settings = ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            settings.CurrentSettings.CurrentGame = gameEnum;

            var game = ServiceProvider.GetRequiredService<IWpfGame>();
            var resourceLibrary = ServiceProvider.GetRequiredService<ResourceLibrary>();
            resourceLibrary.Initialize(game.GraphicsDevice, game.Content);

            PackFileService = ServiceProvider.GetRequiredService<IPackFileService>();
            CommandFactory = ServiceProvider.GetRequiredService<IUiCommandFactory>();
            ScopeRepository = ServiceProvider.GetRequiredService<IScopeRepository>() ;
        }

        public PackFileContainer? LoadPackFile(string path, bool createOutputPackFile = true)
        {
            var loader = ServiceProvider.GetRequiredService<IPackFileContainerLoader>();
            var container = loader.Load(path);
            PackFileService.AddContainer(container);

            if (createOutputPackFile)
                return PackFileService.CreateNewPackFileContainer("TestOutput", PackFileCAType.MOD, true);
            return null;
        }

        public PackFileContainer? CreateCaContainer()
        {
            var caConainter = new PackFileContainer("CA")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\files\game\ca.pack"
            };
            PackFileService.AddContainer(caConainter, false);
            return caConainter;
        }

        public T GetRequiredServiceInCurrentEditorScope<T>()
        {
            var handle = ScopeRepository.GetEditorHandles().First();
            return ScopeRepository.GetRequiredService<T>(handle);
        }

        public PackFileContainer LoadFolderPackFile(string path)
        {
            var loader = ServiceProvider.GetRequiredService<IPackFileContainerLoader>();
            var container = loader.LoadSystemFolderAsPackFileContainer(path);

            PackFileService.AddContainer(container);
            return container;
        }

        public PackFileContainer CreateOutputPack()
        {
            return PackFileService.CreateNewPackFileContainer("TestOutput", PackFileCAType.MOD, true);
        }


        public PackFileContainer CreateEmptyPackFile(string packFileName, bool setAsEditable)
        {
            return PackFileService.CreateNewPackFileContainer(packFileName, PackFileCAType.MOD, setAsEditable);
        }

        void MockServices(IServiceCollection services)
        {
            // Find a way to disable the whole rendering loop, we dont want it! 

            var gameDescriptor = new ServiceDescriptor(typeof(IWpfGame), typeof(WpfGame), ServiceLifetime.Scoped);
            services.Remove(gameDescriptor);
            services.AddScoped<IWpfGame, WpfGameMock>();

            var keyboardDescriptor = new ServiceDescriptor(typeof(IKeyboardComponent), typeof(KeyboardComponent), ServiceLifetime.Scoped);
            services.Remove(keyboardDescriptor);
            services.AddScoped(x => new Mock<IKeyboardComponent>().Object);

            var mouseDescriptor = new ServiceDescriptor(typeof(IMouseComponent), typeof(MouseComponent), ServiceLifetime.Scoped);
            services.Remove(mouseDescriptor);
            services.AddScoped(x => new Mock<IMouseComponent>().Object);


            Dialogs = new Mock<IStandardDialogs>();
            var dialogDescriptor = new ServiceDescriptor(typeof(IStandardDialogs), typeof(StandardDialogs), ServiceLifetime.Scoped);
            services.Remove(dialogDescriptor);
            services.AddScoped(x => Dialogs.Object);
        }
    }
}
