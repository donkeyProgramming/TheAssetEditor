using Editors.Audio.Shared.AudioProject;
using Editors.MusicProjectEditor.ViewModels;
using Editors.MusicProjectEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.MusicProjectEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MusicProjectEditorView>();
            serviceCollection.AddTransient<MusicProjectEditorViewModel>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<MusicProjectEditorViewModel, MusicProjectEditorView>(EditorEnums.MusicProject_Editor)
                .AddExtention(MusicAudioProjectService.FileExtension, EditorPriorites.High)
                .AddToToolbar("Music Project Editor")
                .Build(factory);
        }
    }
}
