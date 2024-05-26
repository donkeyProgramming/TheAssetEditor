using Audio.BnkCompiler;
using Audio.BnkCompiler.ObjectGeneration;
using Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Audio.FileFormats.WWise;
using Audio.Presentation.AudioExplorer;
using Audio.Presentation.Compiler;
using Audio.Storage;
using Audio.Utility;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using System.IO;
using System.Linq;

namespace Audio
{
    public class AudioEditor_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<VgStreamWrapper>();

            serviceCollection.AddScoped<AudioEditorMainView>();
            serviceCollection.AddScoped<AudioEditorViewModel>();

            serviceCollection.AddScoped<CompilerView>();
            serviceCollection.AddScoped<CompilerViewModel>();

            serviceCollection.AddScoped<RepositoryProvider, CreateRepositoryFromAllPackFiles>();
            serviceCollection.AddScoped<IAudioRepository, AudioRepository>();

            serviceCollection.AddTransient<WWiseBnkLoader>();
            serviceCollection.AddTransient<WWiseNameLoader>();
            serviceCollection.AddTransient<BnkParser>();

            serviceCollection.AddTransient<SoundPlayer>();
            serviceCollection.AddTransient<AudioResearchHelper>();

            serviceCollection.AddScoped<IWWiseHircGenerator, ActionGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, EventGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, SoundGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, ActorMixerGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, RandomContainerGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, DialogueEventGenerator>();
            serviceCollection.AddScoped<HichBuilder>();
            serviceCollection.AddScoped<BnkHeaderBuilder>();
            serviceCollection.AddScoped<CompilerService>();
            serviceCollection.AddScoped<ProjectLoader>();
            serviceCollection.AddScoped<AudioFileImporter>();
            serviceCollection.AddScoped<Compiler>();
            serviceCollection.AddScoped<ResultHandler>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AudioEditorViewModel, AudioEditorMainView>();
            factory.RegisterTool<CompilerViewModel, CompilerView>();// ( new ExtentionToTool( EditorEnums.AudioCompiler_Editor,  new[] { ".audio_json"}));
        }
    }

    public static class AudioTool_Debug
    {
        public static void CreateOvnCompilerProject(PackFileService pfs)
        {
            PackFileUtil.LoadFilesFromDisk(pfs, new PackFileUtil.FileRef(packFilePath: @"audioprojects", systemPath: @"C:\Users\ole_k\source\repos\TheAssetEditor\AudioResearch\Data\OvnExample\ProjectSimple.json"));

            // Load all wems
            var wemReferences = Directory.GetFiles(@"D:\Research\Audio\Working pack\audio_ovn\wwise\english(uk)")
                .Where(x => Path.GetExtension(x) == ".wem")
                .Select(x => new PackFileUtil.FileRef(packFilePath: @"audio\wwise", systemPath: x))
                .ToList();
            PackFileUtil.LoadFilesFromDisk(pfs, wemReferences);
        }
    }



}

/*
 * 
 * Final test, add a new sound in meta tabel Karl franze running : "Look at me....Wiiiii" 
 * Vocalisation_dlc14_medusa_idle_hiss
 * 
    event > action > sound > .wem
    event > action > random-sequence > sound(s) > .wem
    event > action > switch > switch/segment/sound > ...
    event > action > music segment > music track(s) > .wem(s).
    event > action > music random-sequence > music segment(s) > ...
    event > action > music switch > switch(es)/segment(s)/random-sequence(s) > ...


    Event => action     =>  sound
                        =>  CAkActionSetAkProp
                        =>  Switch  => sound
                                    => Rand

                        =>  Rand    => Sound
 */


