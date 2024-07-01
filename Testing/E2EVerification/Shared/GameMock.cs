using System.Windows;
using GameWorld.WpfWindow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace E2EVerification.Shared
{
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
}
