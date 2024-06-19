using Audio.Presentation.AudioExplorer;
using Audio.Presentation.Compiler;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Editors.Audio.Presentation.AudioEditor;
using Editors.Audio.Presentation.AudioEditor.ViewModels;
using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Presentation.Compiler;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.GameFormats.WWise;
using System.IO;
using System.Linq;

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

            serviceCollection.AddScoped<AudioEditorDataView>();
            serviceCollection.AddScoped<AudioEditorDataViewModel>();
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
            serviceCollection.AddScoped<Compiler>();
            serviceCollection.AddScoped<ResultHandler>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AudioExplorerViewModel, AudioExplorerView>();
            factory.RegisterTool<CompilerViewModel, CompilerView>();
            factory.RegisterTool<AudioEditorViewModel, AudioEditorView>();
        }
    }
}
