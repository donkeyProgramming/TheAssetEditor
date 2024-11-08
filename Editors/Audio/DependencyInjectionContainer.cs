using Audio.AudioExplorer;
using Audio.Compiler;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.AudioExplorer;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Editors.Audio.Compiler;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.GameFormats.WWise;

namespace Editors.Audio
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<VgStreamWrapper>();

            serviceCollection.AddScoped<AudioExplorerView>();
            serviceCollection.AddScoped<AudioExplorerViewModel>();

            serviceCollection.AddScoped<CompilerView>();
            serviceCollection.AddScoped<CompilerViewModel>();

            serviceCollection.AddTransient<AudioEditorSettingsView>();
            serviceCollection.AddScoped<AudioEditorSettingsViewModel>();
            serviceCollection.AddScoped<AudioEditorViewModel>();

            serviceCollection.AddScoped<RepositoryProvider, CreateRepositoryFromAllPackFiles>();
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();

            serviceCollection.AddTransient<BnkLoader>();
            serviceCollection.AddTransient<DatLoader>();
            serviceCollection.AddTransient<BnkParser>();
            serviceCollection.AddTransient<SoundPlayer>();

            serviceCollection.AddScoped<IWWiseHircGenerator, ActionGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, EventGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, SoundGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, ActorMixerGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, RandomContainerGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, DialogueEventGenerator>();
            serviceCollection.AddScoped<HircBuilder>();
            serviceCollection.AddScoped<BnkHeaderBuilder>();
            serviceCollection.AddScoped<CompilerService>();
            serviceCollection.AddScoped<ProjectLoader>();
            serviceCollection.AddScoped<AudioFileImporter>();
            serviceCollection.AddScoped<BnkCompiler.Compiler>();
            serviceCollection.AddScoped<ResultHandler>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<AudioExplorerViewModel, AudioExplorerView>(EditorEnums.AudioExplorer_Editor)
                .AddToToolbar("Audio Exporer")
                .Build(factory);

            EditorInfoBuilder
                .Create<CompilerViewModel, CompilerView>(EditorEnums.AudioCompiler_Editor)
                .AddToToolbar("Audio Compiler")
                .Build(factory);

            EditorInfoBuilder
                .Create<AudioEditorViewModel, AudioEditorView>(EditorEnums.Audio_Editor)
                .AddToToolbar("Audio Editor")
                .Build(factory);
        }
    }
}
