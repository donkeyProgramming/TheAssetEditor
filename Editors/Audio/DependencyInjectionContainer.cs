using System.Diagnostics;
using Audio.AudioExplorer;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioProjectViewer.Table;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;
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
            // Audio Editor stuff
            serviceCollection.AddScoped<IAudioEditorStateService, AudioEditorStateService>();
            serviceCollection.AddScoped<IAudioProjectFileService, AudioProjectFileService>();
            serviceCollection.AddScoped<IAudioProjectIntegrityService, AudioProjectIntegrityService>();

            // Audio Editor View Models
            serviceCollection.AddScoped<AudioEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectExplorerViewModel>();
            serviceCollection.AddScoped<AudioFilesExplorerViewModel>();
            serviceCollection.AddScoped<AudioProjectEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectViewerViewModel>();
            serviceCollection.AddScoped<SettingsViewModel>();

            // New Audio Project
            serviceCollection.AddTransient<NewAudioProjectViewModel>();
            serviceCollection.AddTransient<NewAudioProjectWindow>();

            // Audio Project Converter tool
            serviceCollection.AddTransient<AudioProjectConverterViewModel>();
            serviceCollection.AddTransient<AudioProjectConverterWindow>();

            // Audio Editor Commands
            serviceCollection.AddScoped<OpenMovieFileSelectionWindowCommand>();
            serviceCollection.AddScoped<OpenNewAudioProjectWindowCommand>();
            serviceCollection.AddScoped<OpenAudioProjectConverterWindowCommand>();
            serviceCollection.AddScoped<SetAudioFilesCommand>();
            serviceCollection.AddScoped<PlayAudioFileCommand>();
            serviceCollection.AddScoped<AddEditorRowToViewerCommand>();
            serviceCollection.AddScoped<RemoveViewerRowsCommand>();
            serviceCollection.AddScoped<EditViewerRowCommand>();
            serviceCollection.AddScoped<PasteViewerRowsCommand>();
            serviceCollection.AddScoped<IAudioProjectMutationUICommandFactory, AudioProjectMutationUICommandFactory>();
            RegisterAllAsInterface<IAudioProjectMutationUICommand>(serviceCollection, ServiceLifetime.Transient);

            // Audio Project Explorer services
            serviceCollection.AddScoped<IAudioProjectTreeBuilderService, AudioProjectTreeBuilderService>();
            serviceCollection.AddScoped<IAudioProjectTreeFilterService, AudioProjectTreeFilterService>();

            // Audio Files Explorer services
            serviceCollection.AddSingleton<IAudioFilesTreeBuilderService, AudioFilesTreeBuilderService>();
            serviceCollection.AddSingleton<IAudioFilesTreeSearchFilterService, AudioFilesTreeFilterService>();

            // Audio Project Editor table
            serviceCollection.AddScoped<IEditorTableServiceFactory, EditorTableServiceFactory>();
            serviceCollection.AddScoped<IEditorTableService, EditorActionEventDataGridService>();
            serviceCollection.AddScoped<IEditorTableService, EditorDialogueEventDataGridService>();
            serviceCollection.AddScoped<IEditorTableService, EditorStateGroupDataGridService>();

            // Audio Project Viewer services
            serviceCollection.AddScoped<IViewerTableServiceFactory, ViewerTableServiceFactory>();
            serviceCollection.AddScoped<IViewerTableService, ViewerActionEventDataGridService>();
            serviceCollection.AddScoped<IViewerTableService, ViewerDialogueEventDataGridService>();
            serviceCollection.AddScoped<IViewerTableService, ViewerStateGroupDataGridService>();

            // Audio Project mutation
            serviceCollection.AddSingleton<ISoundFactory, SoundFactory>();
            serviceCollection.AddSingleton<IRandomSequenceContainerFactory, RandomSequenceContainerFactory>();
            serviceCollection.AddSingleton<IActionEventFactory, ActionEventFactory>();
            serviceCollection.AddSingleton<IStatePathFactory, StatePathFactory>();
            serviceCollection.AddScoped<IActionEventService, ActionEventService>();
            serviceCollection.AddScoped<IDialogueEventService, DialogueEventService>();
            serviceCollection.AddScoped<IStateService, StateService>();

            // Audio Project Compiler
            serviceCollection.AddScoped<IAudioProjectCompilerService, AudioProjectCompilerService>();
            serviceCollection.AddScoped<CompilerDataProcessor>();
            serviceCollection.AddScoped<SoundBankGenerator>();
            serviceCollection.AddScoped<WemGenerator>();
            serviceCollection.AddScoped<DatGenerator>();

            // Audio Explorer
            serviceCollection.AddScoped<AudioExplorerViewModel>();

            // Shared audio stuff 
            serviceCollection.AddSingleton<IAudioRepository, AudioRepository>();
            serviceCollection.AddSingleton<BnkLoader>();
            serviceCollection.AddSingleton<DatLoader>();
            serviceCollection.AddSingleton<BnkParser>();
            serviceCollection.AddScoped<SoundPlayer>();
            serviceCollection.AddScoped<VgStreamWrapper>();
            serviceCollection.AddScoped<WSourcesWrapper>();

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
