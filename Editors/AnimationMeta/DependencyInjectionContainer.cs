using AnimationEditor.Common.BaseControl;
using Editors.AnimationMeta.MetaEditor.Commands;
using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.Presentation.View;
using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.Visualisation;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<MetaDataTagDeSerializer>();
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<MetaDataEditorViewModel>();

            serviceCollection.AddScoped<EditorHost<SuperViewViewModel>>();
            serviceCollection.AddScoped<SuperViewViewModel>();

            serviceCollection.AddScoped<IMetaDataFactory, MetaDataFactory>(); // Needs heavy refactorying!

            // Commands for metadata editor
            serviceCollection.AddTransient<CopyPastCommand>();
            serviceCollection.AddTransient<DeleteEntryCommand>();
            serviceCollection.AddTransient<MoveEntryCommand>();
            serviceCollection.AddTransient<NewEntryCommand>();
            serviceCollection.AddTransient<SaveCommand>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<SuperViewViewModel, EditorHostView>(EditorEnums.SuperView_Editor)
                .AddToToolbar("SuperView")
                .Build(factory);

            EditorInfoBuilder
                .Create<MetaDataEditorViewModel, MainEditorView> (EditorEnums.Meta_Editor)
                .AddExtention(".anm.meta", EditorPriorites.High)
                .AddExtention(".meta", EditorPriorites.High)
                .AddExtention(".snd.meta", EditorPriorites.High)
                .Build(factory);
        }
    }
}
