using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using KitbasherEditor.Views.EditorViews.Rmv2;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Services;
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
        [ObservableProperty] MaterialGeneralViewModel? _materialGeneral;
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

            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3)
            {
                WsMaterial = _serviceProvider.GetRequiredService<WsMaterialViewModel>();
                WsMaterial.Initialize(typedNode);
            }
            //else
            {
                if (typedNode.Material is WeightedMaterial)
                {
                    MaterialGeneral = _serviceProvider.GetRequiredService<MaterialGeneralViewModel>();
                    MaterialGeneral.Initialize(typedNode);

                    Material = _serviceProvider.GetRequiredService<WeightedMaterialViewModel>();
                    Material.Initialize(typedNode);
                }
            }
        }

        public void Dispose()
        {
            Mesh?.Dispose();
            Animation?.Dispose();
        }
    }
}
