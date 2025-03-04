using System.Diagnostics;
using Audio.AudioExplorer;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.AudioEditorMenu;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectCompiler;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.AudioExplorer;
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

            serviceCollection.AddScoped<AudioEditorViewModel>();
            serviceCollection.AddScoped<AudioEditorMenuViewModel>();
            serviceCollection.AddScoped<AudioProjectExplorerViewModel>();
            serviceCollection.AddScoped<AudioFilesExplorerViewModel>();
            serviceCollection.AddScoped<AudioProjectEditorViewModel>();
            serviceCollection.AddScoped<AudioProjectViewerViewModel>();
            serviceCollection.AddScoped<AudioSettingsViewModel>();
            serviceCollection.AddScoped<NewAudioProjectViewModel>();
            serviceCollection.AddScoped<NewAudioProjectWindow>();

            serviceCollection.AddScoped<IAudioProjectService, AudioProjectService>();
            serviceCollection.AddScoped<IntegrityChecker>();
            serviceCollection.AddScoped<AudioProjectCompiler>();

            serviceCollection.AddScoped<RepositoryProvider, CreateRepositoryFromAllPackFiles>();
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();
            serviceCollection.AddScoped<VgStreamWrapper>();
            serviceCollection.AddScoped<WemGenerator>();
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
