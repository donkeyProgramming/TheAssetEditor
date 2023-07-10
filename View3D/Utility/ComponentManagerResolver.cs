using MonoGame.Framework.WpfInterop;

namespace View3D.Utility
{
    public class ComponentManagerResolver
    {
        private readonly WpfGame _game;

        public IComponentManager ComponentManager { get => _game; }
        public ComponentManagerResolver(WpfGame game)
        {
            _game = game;
        }
    }
}
