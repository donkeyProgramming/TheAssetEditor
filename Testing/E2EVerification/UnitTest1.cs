using System;
using System.Windows;
using System.Windows.Forms;
using AssetEditor.Services;
using AssetEditor.ViewModels;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.WpfWindow;
using GameWorld.WpfWindow.ResourceHandling;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.UiCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var _serviceProvider = new DependencyInjectionConfig().Build(ReplaceServices);

   //



            var _rootScope = _serviceProvider.CreateScope();

            var game = _rootScope.ServiceProvider.GetRequiredService<IWpfGame>();
            var rlib = _rootScope.ServiceProvider.GetRequiredService<ResourceLibrary>();
            rlib.Initialize(game);

            var pfs = _rootScope.ServiceProvider.GetRequiredService<PackFileService>();
            pfs.Load("C:\\Users\\ole_k\\source\\repos\\TheAssetEditor\\Data\\Karl_and_celestialgeneral.pack");

            var file = pfs.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");

            //var rootView = _rootScope.ServiceProvider.GetRequiredService<MainViewModel>();
            var commandF = _rootScope.ServiceProvider.GetRequiredService<IUiCommandFactory>();
            commandF.Create<OpenFileInEditorCommand>().Execute(file);


            var scopeF = _rootScope.ServiceProvider.GetRequiredService<ScopeRepository>(); 

            var kitbasher = scopeF.Scopes.First().Value.ServiceProvider.GetRequiredService<KitbasherViewModel>();


            var saveSettings = scopeF.Scopes.First().Value.ServiceProvider.GetRequiredService<SaveSettings>();
            saveSettings.IsUserInitialized = true;


            var kitBasherCOmmandF = scopeF.Scopes.First().Value.ServiceProvider.GetRequiredService<IUiCommandFactory>();
            kitBasherCOmmandF.Create<SaveCommand>().Execute();

            // Verify motherfucker!
            /*
             
           
             */


            //kitbasher.MainFile 
        }

        void ReplaceServices(IServiceCollection services)
        {
           // var keyboard = ;
           // keyboard.Object
           // Find a way to disable the whole rendering loop, we dont want it! 

            var gameDescriptor = new ServiceDescriptor(typeof(IWpfGame), typeof(WpfGame), ServiceLifetime.Scoped);
            services.Remove(gameDescriptor);
            services.AddScoped<IWpfGame, GameMock>();

            var KeyboardDescriptor = new ServiceDescriptor(typeof(IKeyboardComponent), typeof(KeyboardComponent), ServiceLifetime.Scoped);
            services.Remove(KeyboardDescriptor);
            services.AddScoped(x=> new Mock<IKeyboardComponent>().Object);

            var mouseDescriptor = new ServiceDescriptor(typeof(IMouseComponent), typeof(MouseComponent), ServiceLifetime.Scoped);
            services.Remove(mouseDescriptor);
            services.AddScoped(x => new Mock<IMouseComponent>().Object);
        }
    }

    // KeyboardComponent
    // MouseComponent
    // Camera

    public class GameMock : IWpfGame
    {
        public ContentManager Content { get; set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public GameMock()
        {
            var test = new GraphicsDeviceServiceMock();
            GraphicsDevice = test.GraphicsDevice;

            var servies = new GameServiceContainer();
          // servies.AddService(typeof(GraphicsDevice), this);
           servies.AddService(typeof(IGraphicsDeviceService), test);

            Content = new ContentManager(servies, "C:\\Users\\ole_k\\source\\repos\\TheAssetEditor\\GameWorld\\ContentProject\\BuiltContent");
        }

        public T AddComponent<T>(T comp) where T : IGameComponent
        {
            return comp;
            //throw new NotImplementedException();
        }

        public void ForceEnsureCreated()
        {
            //throw new NotImplementedException();
        }

        public FrameworkElement GetFocusElement()
        {
            return null;
            //throw new NotImplementedException();
        }

        public void RemoveComponent<T>(T comp) where T : IGameComponent
        {
            //throw new NotImplementedException();
        }
    }

    public class GraphicsDeviceServiceMock : IGraphicsDeviceService
    {
        GraphicsDevice _GraphicsDevice;
        Form HiddenForm;

        public GraphicsDeviceServiceMock()
        {
            HiddenForm = new Form()
            {
                Visible = false,
                ShowInTaskbar = false
            };

            var Parameters = new PresentationParameters()
            {
                BackBufferWidth = 1280,
                BackBufferHeight = 720,
                DeviceWindowHandle = HiddenForm.Handle,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false
            };

            _GraphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, Parameters);
        }

        public GraphicsDevice GraphicsDevice
        {
            get { return _GraphicsDevice; }
        }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public void Release()
        {
            _GraphicsDevice.Dispose();
            _GraphicsDevice = null;

            HiddenForm.Close();
            HiddenForm.Dispose();
            HiddenForm = null;
        }
    }
}
