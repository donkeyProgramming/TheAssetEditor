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

        public IServiceProvider EditorServiceProvider => _serviceProvider;
        public IPackFileService PackFileService { get; private set; }
        public IUiCommandFactory CommandFactory { get; private set; }
        public IScopeRepository ScopeRepository { get; private set; }
        public Mock<IStandardDialogs> Dialogs { get; private set; }


        public AssetEditorTestRunner(GameTypeEnum gameEnum = GameTypeEnum.Warhammer3, bool forceValidateServiceScopes = false)
        {
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes, MockServices);
  
            var settings = EditorServiceProvider.GetRequiredService<ApplicationSettingsService>();
            settings.CurrentSettings.CurrentGame = gameEnum;

            var game = EditorServiceProvider.GetRequiredService<IWpfGame>();
            var resourceLibrary = EditorServiceProvider.GetRequiredService<ResourceLibrary>();
            resourceLibrary.Initialize(game.GraphicsDevice, game.Content);

            PackFileService = EditorServiceProvider.GetRequiredService<IPackFileService>();
            CommandFactory = EditorServiceProvider.GetRequiredService<IUiCommandFactory>();
            ScopeRepository = EditorServiceProvider.GetRequiredService<IScopeRepository>() ;
            //(ScopeRepository as ScopeRepository).Root = EditorServiceProvider;
        }

        public PackFileContainer? LoadPackFile(string path, bool createOutputPackFile = true)
        {
            var loader = EditorServiceProvider.GetRequiredService<IPackFileContainerLoader>();
            var container = loader.Load(path);
            container.IsCaPackFile = true;
            PackFileService.AddContainer(container);

            if (createOutputPackFile)
                return PackFileService.CreateNewPackFileContainer("TestOutput", PackFileCAType.MOD, true);
            return null;
        }

        public T GetRequiredService<T>()
        {
            var handle = ScopeRepository.GetEditorHandles().First();
            return ScopeRepository.GetRequiredService<T>(handle);
        }


        public PackFileContainer? LoadFolderPackFile(string path, bool createOutputPackFile = true)
        {
            var loader = EditorServiceProvider.GetRequiredService<IPackFileContainerLoader>();
            var container = loader.LoadSystemFolderAsPackFileContainer(path);
            container.IsCaPackFile = true;
            PackFileService.AddContainer(container);

            if (createOutputPackFile)
                return PackFileService.CreateNewPackFileContainer("TestOutput", PackFileCAType.MOD, true);
            return null;
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
