using Audio.AudioEditor;
using Audio.BnkCompiler.ObjectGeneration;
using Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Audio.FileFormats.WWise;
using Audio.Presentation;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
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
            
            serviceCollection.AddTransient<WWiseBnkLoader>();
            serviceCollection.AddTransient<WWiseNameLoader>();
            serviceCollection.AddTransient<Bnkparser>();

            serviceCollection.AddTransient<SoundPlayer>();
            serviceCollection.AddTransient<AudioResearchHelper>();

            serviceCollection.AddScoped<IWWiseHircGenerator, ActionGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, EventGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, SoundGenerator>();
            serviceCollection.AddScoped<IWWiseHircGenerator, ActorMixerGenerator>();
            serviceCollection.AddScoped<HichBuilder>();
            serviceCollection.AddScoped<BnkHeaderBuilder>();
            serviceCollection.AddScoped<Compiler>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AudioEditorViewModel, AudioEditorMainView>();
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


