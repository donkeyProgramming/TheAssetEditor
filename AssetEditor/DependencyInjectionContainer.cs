﻿using AssetEditor.Services;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor
{
    internal class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<MainWindow>();
            serviceCollection.AddScoped<MainViewModel>();
            serviceCollection.AddScoped<IEditorCreator, EditorCreator>();

            serviceCollection.AddTransient<DeepSearchCommand>();
            serviceCollection.AddTransient<GenerateReportCommand>();
            serviceCollection.AddTransient<OpenGamePackCommand>();
            serviceCollection.AddTransient<OpenPackFileCommand>();
            serviceCollection.AddTransient<OpenSettingsDialogCommand>();
            serviceCollection.AddTransient<OpenWebpageCommand>();
            serviceCollection.AddTransient<OpenEditorCommand>();
            serviceCollection.AddTransient<OpenFileInEditorCommand>();
            serviceCollection.AddTransient<OpenFileInWindowedEditorCommand>();

            serviceCollection.AddTransient<SettingsWindow>();
            serviceCollection.AddScoped<SettingsViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();
        }
    }
}
