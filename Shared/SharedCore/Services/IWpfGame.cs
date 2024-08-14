using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shared.Core.Services
{
    public interface IWpfGame
    {
        ContentManager Content { get; set; }
        GraphicsDevice GraphicsDevice { get; }

        void ForceEnsureCreated();
        FrameworkElement GetFocusElement();

        T AddComponent<T>(T comp) where T : IGameComponent;
        void RemoveComponent<T>(T comp) where T : IGameComponent;
    }
}
