using System;
using GameWorld.WpfWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.WindowHandling
{
    public class WindowFactory : IWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WpfGame _wpfGame;

        public WindowFactory(IServiceProvider serviceProvider, WpfGame wpfGame)
        {
            _serviceProvider = serviceProvider;
            _wpfGame = wpfGame;
        }

        public ITypedAssetEditorWindow<TViewModel> Create<TViewModel, TView>(string title, int initialWidth, int initialHeight)
            where TViewModel : class
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            var view = _serviceProvider.GetRequiredService<TView>();

            var containingWindow = new AssetEditorWindow<TViewModel>
            {
                Title = title,
                Width = initialWidth,
                Height = initialHeight,
                DataContext = viewModel,
                Content = view,
                AlwaysOnTop = true,
            };

            containingWindow.Init();

            if (viewModel is IGameComponent component)
            {
                _wpfGame.AddComponent(component);
                containingWindow.Closed += (x, y) => OnComponentRemoved(component);
            }

            return containingWindow;
        }

        void OnComponentRemoved(IGameComponent component)
        {
        }
    }


}
