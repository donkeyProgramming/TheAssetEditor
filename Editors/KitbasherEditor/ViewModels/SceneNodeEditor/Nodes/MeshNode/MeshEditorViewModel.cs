using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views.EditorViews.Rmv2;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.Ui.Common.DataTemplates;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class MeshEditorViewModel : ObservableObject, ISceneNodeEditor, IViewProvider<MeshEditorView>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationSettingsService _applicationSettingsService;

        [ObservableProperty] MeshViewModel? _mesh;
        [ObservableProperty] AnimationViewModel? _animation;
        [ObservableProperty] WeightedMaterialViewModel? _material;
        [ObservableProperty] WsMaterialViewModel? _wsMaterial;

        public MeshEditorViewModel(IServiceProvider serviceProvider, ApplicationSettingsService applicationSettingsService)
        {
            _serviceProvider = serviceProvider;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Initialize(ISceneNode node)
        {
            var typedNode = node as Rmv2MeshNode;
            Guard.IsNotNull(typedNode);

            Mesh = _serviceProvider.GetRequiredService<MeshViewModel>();
            Mesh.Initialize(typedNode);

            Animation = _serviceProvider.GetRequiredService<AnimationViewModel>();
            Animation.Initialize(typedNode);

            WsMaterial = _serviceProvider.GetRequiredService<WsMaterialViewModel>();
            WsMaterial.Initialize(typedNode);
            
            if (typedNode.RmvMaterial is WeightedMaterial)
            {
                Material = _serviceProvider.GetRequiredService<WeightedMaterialViewModel>();
                Material.Initialize(typedNode);
            }
        }

        public void Dispose()
        {
            Animation?.Dispose();
        }
    }
}
