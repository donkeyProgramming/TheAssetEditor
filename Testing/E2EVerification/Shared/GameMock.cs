using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;

namespace E2EVerification.Shared
{
    public class GameMock : IWpfGame
    {
        public ContentManager Content { get; set; }
        public GraphicsDevice GraphicsDevice { get; private set; }

        public GameMock()
        {
            var test = new GraphicsDeviceServiceMock();
            GraphicsDevice = test.GraphicsDevice;
            
            var services = new GameServiceContainer();
            services.AddService(typeof(IGraphicsDeviceService), test);

            var fullPath = Path.GetFullPath(@"..\..\..\..\..\GameWorld\ContentProject\bin\Debug\net9.0-windows\Content");
            if (Directory.Exists(fullPath) == false)
                throw new Exception("Unable to determine full path of content folder");

            Content = new ContentManager(services, fullPath);
        }

        public T AddComponent<T>(T comp) where T : IGameComponent
        {
            return comp;
        }

        public void ForceEnsureCreated()
        {
        }

        public FrameworkElement GetFocusElement()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public void RemoveComponent<T>(T comp) where T : IGameComponent
        {
        }
    }
}
