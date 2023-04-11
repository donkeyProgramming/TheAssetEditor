using Audio.AudioEditor;
using Audio.Presentation;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Audio
{
    public class AudioEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<AudioEditorMainView>();
            serviceCollection.AddScoped<AudioEditorViewModel>();

            serviceCollection.AddScoped<RepositoryProvider, CreateRepositoryFromAllPackFiles>();
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();
            
            serviceCollection.AddTransient<WwiseDataLoader>();
            serviceCollection.AddTransient<SoundPlayer>();
            serviceCollection.AddTransient<AudioDebugExportHelper>();
            
            // Clanup loading

        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AudioEditorViewModel, AudioEditorMainView>();
          //  factory.RegisterFileTool<AudioEditorViewModel, AudioEditorMainView>(new ExtentionToTool(EditorEnums.AudioExplorer_Editor, new[] { ".bnk" }));
        }
    }
}
