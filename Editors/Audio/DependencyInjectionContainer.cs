using System.Diagnostics;
using Audio.AudioExplorer;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.AudioExplorer;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.AudioProjectConverter;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Wwise;

namespace Editors.Audio
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<AudioExplorerView>();
            serviceCollection.AddScoped<AudioExplorerViewModel>();
            
            serviceCollection.AddScoped<IAudioEditorService, AudioEditorService>();
            serviceCollection.AddScoped<AudioEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectExplorerViewModel>();
            serviceCollection.AddScoped<AudioFilesExplorerViewModel>();
            serviceCollection.AddScoped<AudioProjectEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectViewerViewModel>();
            serviceCollection.AddScoped<SettingsViewModel>();
            serviceCollection.AddScoped<IntegrityChecker>();

            serviceCollection.AddScoped<IDataGridServiceFactory, DataGridServiceFactory>();
            serviceCollection.AddTransient<IDataGridService, EditorActionEventDataGridService>();
            serviceCollection.AddTransient<IDataGridService, EditorDialogueEventDataGridService>();
            serviceCollection.AddTransient<IDataGridService, EditorStateGroupDataGridService>();
            serviceCollection.AddTransient<IDataGridService, ViewerActionEventDataGridService>();
            serviceCollection.AddTransient<IDataGridService, ViewerDialogueEventDataGridService>();
            serviceCollection.AddTransient<IDataGridService, ViewerStateGroupDataGridService>();

            serviceCollection.AddScoped<IAudioProjectUICommandFactory, AudioProjectUICommandFactory>();
            serviceCollection.AddTransient<IAudioProjectUICommand, AddActionEventToAudioProjectCommand>();
            serviceCollection.AddTransient<IAudioProjectUICommand, AddDialogueEventToAudioProjectCommand>();
            serviceCollection.AddTransient<IAudioProjectUICommand, AddStateToAudioProjectCommand>();
            serviceCollection.AddTransient<IAudioProjectUICommand, RemoveActionEventFromAudioProjectCommand>();
            serviceCollection.AddTransient<IAudioProjectUICommand, RemoveDialogueEventFromAudioProjectCommand>();
            serviceCollection.AddScoped<CopyRowsCommand>();
            serviceCollection.AddScoped<PasteRowsCommand>();
            serviceCollection.AddScoped<SelectMovieFileCommand>();

            serviceCollection.AddSingleton<ISoundFactory, SoundFactory>();
            serviceCollection.AddSingleton<IRandomSequenceContainerFactory, RandomSequenceContainerFactory>();
            serviceCollection.AddSingleton<IActionEventFactory, ActionEventFactory>();
            serviceCollection.AddSingleton<IStatePathFactory, StatePathFactory>();

            serviceCollection.AddTransient<NewAudioProjectViewModel>();
            serviceCollection.AddTransient<NewAudioProjectWindow>();
            serviceCollection.AddScoped<OpenNewAudioProjectWindowCommand>();

            serviceCollection.AddTransient<AudioProjectConverterViewModel>();
            serviceCollection.AddTransient<AudioProjectConverterWindow>();
            serviceCollection.AddScoped<OpenAudioProjectConverterWindowCommand>();

            serviceCollection.AddScoped<CompilerDataProcessor>();
            serviceCollection.AddScoped<SoundBankGenerator>();
            serviceCollection.AddScoped<WemGenerator>();
            serviceCollection.AddScoped<DatGenerator>();

            serviceCollection.AddScoped<RepositoryProvider, CreateRepositoryFromAllPackFiles>();
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();
            serviceCollection.AddScoped<VgStreamWrapper>();
            serviceCollection.AddScoped<WSourcesWrapper>();
            serviceCollection.AddScoped<BnkLoader>();
            serviceCollection.AddScoped<DatLoader>();
            serviceCollection.AddScoped<BnkParser>();
            serviceCollection.AddScoped<SoundPlayer>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<AudioExplorerViewModel, AudioExplorerView>(EditorEnums.AudioExplorer_Editor)
                .AddToToolbar("Audio Explorer")
                .Build(factory);


            var enableAudioEditor = false;
            if(Debugger.IsAttached)
                enableAudioEditor = true;
            EditorInfoBuilder
                .Create<AudioEditorViewModel, AudioEditorView>(EditorEnums.Audio_Editor)
                .AddToToolbar("Audio Editor", enableAudioEditor)
                .Build(factory);
        }
    }
}
