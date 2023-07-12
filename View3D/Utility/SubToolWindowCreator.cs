using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using System;
using System.Windows;
using System.Windows.Controls;
using View3D.Services;

namespace View3D.Utility
{
    public class SubToolWindowCreator
    {
        private readonly IServiceProvider _serviceProvider;

        public SubToolWindowCreator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void CreateComponentWindow<TUserView, TViewModel>(string header, int width, int heigh)
            where TViewModel : IGameComponent
            where TUserView : UserControl
        {
            var scene = _serviceProvider.GetRequiredService<GameWorld>();
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            scene.AddComponent(viewModel);

            var containingWindow = new Window();
            containingWindow.Title = header;
            containingWindow.Width = width;
            containingWindow.Height = heigh;
            containingWindow.DataContext = viewModel;
            containingWindow.Content = _serviceProvider.GetRequiredService<TUserView>();
            containingWindow.Closed += (x, y) => OnComponentRemoved(viewModel);

            containingWindow.Show();
        }

        void OnComponentRemoved(IGameComponent component)
        {
            var scene = _serviceProvider.GetRequiredService<GameWorld>();
            scene.RemoveComponent(component);

            if (component is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
