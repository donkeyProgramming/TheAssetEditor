using System.Windows;
using GameWorld.WpfWindow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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

            var fullPath = Path.GetFullPath(@"..\..\..\..\..\GameWorld\ContentProject\BuiltContent");
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
            return null;
        }

        public void RemoveComponent<T>(T comp) where T : IGameComponent
        {
        }
    }
}
