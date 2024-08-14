﻿using AssetEditor.Services;
using GameWorld.Core.Components.Input;
using GameWorld.Core.WpfWindow;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace E2EVerification.Shared
{
    public class AssetEditorTestRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public IServiceScope EditorServiceProvider { get; private set; }
        public PackFileService PackFileService { get; private set; }
        public IUiCommandFactory CommandFactory { get; private set; }
        public ScopeRepository ScopeRepository { get; private set; }

        public AssetEditorTestRunner(GameTypeEnum gameEnum = GameTypeEnum.Warhammer3, bool forceValidateServiceScopes = false)
        {
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes, MockServices);
            EditorServiceProvider = _serviceProvider.CreateScope();

            var settings = EditorServiceProvider.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            settings.CurrentSettings.CurrentGame = gameEnum;
            
            var game = EditorServiceProvider.ServiceProvider.GetRequiredService<IWpfGame>();
            var resourceLibrary = EditorServiceProvider.ServiceProvider.GetRequiredService<ResourceLibrary>();
            resourceLibrary.Initialize(game.GraphicsDevice, game.Content);

            PackFileService = EditorServiceProvider.ServiceProvider.GetRequiredService<PackFileService>();
            CommandFactory = EditorServiceProvider.ServiceProvider.GetRequiredService<IUiCommandFactory>();
            ScopeRepository = EditorServiceProvider.ServiceProvider.GetRequiredService<ScopeRepository>();
        }

        public PackFileContainer? LoadPackFile(string path, bool createOutputPackFile = true)
        {
            PackFileService.Load(path, false, true);
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
            services.AddScoped<IWpfGame, GameMock>();

            var keyboardDescriptor = new ServiceDescriptor(typeof(IKeyboardComponent), typeof(KeyboardComponent), ServiceLifetime.Scoped);
            services.Remove(keyboardDescriptor);
            services.AddScoped(x => new Mock<IKeyboardComponent>().Object);

            var mouseDescriptor = new ServiceDescriptor(typeof(IMouseComponent), typeof(MouseComponent), ServiceLifetime.Scoped);
            services.Remove(mouseDescriptor);
            services.AddScoped(x => new Mock<IMouseComponent>().Object);
        }
    }
}
