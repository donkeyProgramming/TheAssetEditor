using Editors.MusicDatEditor.ViewModels;
using Editors.MusicDatEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.MusicDatEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MusicDatEditorView>();
            serviceCollection.AddTransient<MusicDatEditorViewModel>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            // "campaign_music.dat" / "battle_music.dat" have only one dot, so the generic
            // extension match resolves to plain ".dat" - scope it to just these two known file
            // names, the same way DatLoader.cs excludes them by exact name from its own
            // (unrelated) sound-bank .dat loader.
            EditorInfoBuilder
                .Create<MusicDatEditorViewModel, MusicDatEditorView>(EditorEnums.MusicDat_Editor)
                .AddExtention(".dat", EditorPriorites.High)
                .ValidForFoldersContaining("campaign_music.dat")
                .ValidForFoldersContaining("battle_music.dat")
                .AddToToolbar("Music Script Viewer")
                .Build(factory);
        }
    }
}
