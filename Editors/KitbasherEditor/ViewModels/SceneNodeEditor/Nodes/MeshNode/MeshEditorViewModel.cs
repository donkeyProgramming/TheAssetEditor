using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using KitbasherEditor.Views.EditorViews.Rmv2;
using Microsoft.Extensions.DependencyInjection;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.Ui.Common.DataTemplates;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class MeshEditorViewModel : ObservableObject, ISceneNodeEditor, IViewProvider<MeshEditorView>
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] MeshViewModel? _mesh;
        [ObservableProperty] AnimationViewModel? _animation;
        [ObservableProperty] MaterialGeneralViewModel? _materialGeneral;
        [ObservableProperty] WeightedMaterialViewModel? _material;

        public MeshEditorViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(ISceneNode node)
        {
            var typedNode = node as Rmv2MeshNode;
            Guard.IsNotNull(typedNode);

            Mesh = _serviceProvider.GetRequiredService<MeshViewModel>();
            Mesh.Initialize(typedNode);

            Animation = _serviceProvider.GetRequiredService<AnimationViewModel>();
            Animation.Initialize(typedNode);

            MaterialGeneral = _serviceProvider.GetRequiredService<MaterialGeneralViewModel>();
            MaterialGeneral.Initialize(typedNode);

            if (typedNode.Material is WeightedMaterial)
            {
                Material = _serviceProvider.GetRequiredService<WeightedMaterialViewModel>();
                Material.Initialize(typedNode);
            }
        }

        public void Dispose()
        {
            Mesh?.Dispose();
            Animation?.Dispose();
        }
    }
}
