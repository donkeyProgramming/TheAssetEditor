using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views.Settings;
using AssetEditor.Views;
using CommonControls;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetEditor
{
    internal class AssetEditor_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<MainWindow>();
            serviceCollection.AddScoped<MainViewModel>();
            serviceCollection.AddScoped<IEditorCreator, EditorCreator>();

            serviceCollection.AddTransient<OpenEditorCommand>();
            serviceCollection.AddTransient<OpenFileInEditorCommand>();

            serviceCollection.AddScoped<SettingsWindow>();
            serviceCollection.AddScoped<SettingsViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();

            serviceCollection.AddTransient<DevelopmentConfiguration>();
        }
    }
}
