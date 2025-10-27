using Audio.AudioExplorer;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation;
using Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer.Table;
using Editors.Audio.AudioEditor.Presentation.NewAudioProject;
using Editors.Audio.AudioEditor.Presentation.Settings;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.AudioEditor.Presentation.WaveformVisualiser;
using Editors.Audio.AudioExplorer;
using Editors.Audio.AudioProjectConverter;
using Editors.Audio.AudioProjectMerger;
using Editors.Audio.DialogueEventMerger;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.Dat;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Editors.Audio.Shared.Wwise;
using Editors.Audio.Shared.Wwise.Generators;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editors.Audio
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Audio Editor stuff
            serviceCollection.AddScoped<IAudioEditorStateService, AudioEditorStateService>();
            serviceCollection.AddScoped<IAudioEditorFileService, AudioEditorFileService>();
            serviceCollection.AddScoped<IAudioEditorIntegrityService, AudioEditorIntegrityService>();

            // Audio Editor View Models
            serviceCollection.AddScoped<AudioEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectExplorerViewModel>();
            serviceCollection.AddScoped<AudioFilesExplorerViewModel>();
            serviceCollection.AddScoped<AudioProjectEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectViewerViewModel>();
            serviceCollection.AddScoped<SettingsViewModel>();
            var serviceCollection1 = serviceCollection.AddScoped<WaveformVisualiserViewModel>();

            // New Audio Project
            serviceCollection.AddTransient<NewAudioProjectViewModel>();
            serviceCollection.AddTransient<NewAudioProjectWindow>();

            // Audio Project Merger
            serviceCollection.AddTransient<AudioProjectMergerViewModel>();
            serviceCollection.AddTransient<AudioProjectMergerWindow>();

            // Dialogue Event Merger
            serviceCollection.AddTransient<DialogueEventMergerViewModel>();
            serviceCollection.AddTransient<DialogueEventMergerWindow>();

            // Audio Project Converter
            serviceCollection.AddTransient<AudioProjectConverterViewModel>();
            serviceCollection.AddTransient<AudioProjectConverterWindow>();

            // Audio Editor Commands
            serviceCollection.AddScoped<AddEditorRowToViewerCommand>();
            serviceCollection.AddScoped<EditViewerRowCommand>();
            serviceCollection.AddScoped<OpenAudioProjectConverterWindowCommand>();
            serviceCollection.AddScoped<OpenAudioProjectMergerWindowCommand>();
            serviceCollection.AddScoped<OpenDialogueEventMergerWindowCommand>();
            serviceCollection.AddScoped<OpenMovieFileSelectionWindowCommand>();
            serviceCollection.AddScoped<OpenNewAudioProjectWindowCommand>();
            serviceCollection.AddScoped<PasteViewerRowsCommand>();
            serviceCollection.AddScoped<PlayAudioFileCommand>();
            serviceCollection.AddScoped<RemoveViewerRowsCommand>();
            serviceCollection.AddScoped<SetAudioFilesCommand>();
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
            serviceCollection.AddScoped<IEditorTableService, EditorActionEventTableService>();
            serviceCollection.AddScoped<IEditorTableService, EditorDialogueEventTableService>();
            serviceCollection.AddScoped<IEditorTableService, EditorStateGroupTableService>();

            // Audio Project Viewer services
            serviceCollection.AddScoped<IViewerTableServiceFactory, ViewerTableServiceFactory>();
            serviceCollection.AddScoped<IViewerTableService, ViewerActionEventTableService>();
            serviceCollection.AddScoped<IViewerTableService, ViewerDialogueEventTableService>();
            serviceCollection.AddScoped<IViewerTableService, ViewerStateGroupTableService>();

            // Waveform Visualiser services
            //serviceCollection.AddSingleton<IWaveformRenderingService, WaveformRenderingService>();
            //serviceCollection.AddSingleton<IAudioPlayerService, AudioPlayerService>();

            // Audio Project
            serviceCollection.AddScoped<IAudioProjectFileService, AudioProjectFileService>();
            serviceCollection.AddSingleton<ISoundFactory, SoundFactory>();
            serviceCollection.AddSingleton<IRandomSequenceContainerFactory, RandomSequenceContainerFactory>();
            serviceCollection.AddSingleton<IActionEventFactory, ActionEventFactory>();
            serviceCollection.AddSingleton<IStatePathFactory, StatePathFactory>();
            serviceCollection.AddScoped<IActionEventService, ActionEventService>();
            serviceCollection.AddScoped<IDialogueEventService, DialogueEventService>();
            serviceCollection.AddScoped<IStateService, StateService>();

            // Audio Project Compiler
            serviceCollection.AddScoped<IAudioProjectCompilerService, AudioProjectCompilerService>();
            serviceCollection.AddScoped<ISoundBankGeneratorService, SoundBankGeneratorService>();
            serviceCollection.AddScoped<IWemGeneratorService, WemGeneratorService>();
            serviceCollection.AddScoped<IDatGeneratorService, DatGeneratorService>();

            // Audio Explorer
            serviceCollection.AddScoped<AudioExplorerViewModel>();

            // Shared audio stuff 
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();
            serviceCollection.AddSingleton<BnkLoader>();
            serviceCollection.AddSingleton<DatLoader>();
            serviceCollection.AddScoped<SoundPlayer>();
            serviceCollection.AddScoped<VgStreamWrapper>();
            serviceCollection.AddScoped<WSourcesWrapper>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<AudioEditorViewModel, AudioEditorView>(EditorEnums.Audio_Editor)
                .AddToToolbar("Audio Editor")
                .Build(factory);

            EditorInfoBuilder
                .Create<AudioExplorerViewModel, AudioExplorerView>(EditorEnums.AudioExplorer_Editor)
                .AddToToolbar("Audio Explorer")
                .Build(factory);
        }
    }
}
