using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Commands.Bone.Clipboard;
using GameWorld.Core.Commands.Face;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Commands.Vertex;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Geometry.Strategies;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Lod.Strategies;
using GameWorld.Core.Services.SceneSaving.Material;
using GameWorld.Core.Services.SceneSaving.Material.Strategies;
using GameWorld.Core.Utility;
using GameWorld.Core.WpfWindow;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Services;

namespace GameWorld.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Graphics scene
            serviceCollection.AddScoped<IGeometryGraphicsContextFactory, GeometryGraphicsContextFactory>();
            serviceCollection.AddScoped<IWpfGame, WpfGame>();
            serviceCollection.AddScoped<IScopedResourceLibrary, ScopedResourceLibrary>();
            
            serviceCollection.AddSingleton<ResourceLibrary>();

            // Settings
            serviceCollection.AddScoped<GeometrySaveSettings>();
            serviceCollection.AddScoped<SceneRenderParametersStore>();

            // Services
            serviceCollection.AddSingleton<ISkeletonAnimationLookUpHelper, SkeletonAnimationLookUpHelper>();
            serviceCollection.AddScoped<MeshBuilderService>();
            serviceCollection.AddScoped<ViewOnlySelectedService>();
            serviceCollection.AddScoped<FocusSelectableObjectService>();
            serviceCollection.AddScoped<ComplexMeshLoader>();
            serviceCollection.AddTransient<WsModelGeneratorService>();
            serviceCollection.AddTransient<MaterialToWsMaterialFactory>();
            
            serviceCollection.AddScoped<FaceEditor>();
            serviceCollection.AddScoped<ObjectEditor>();
            serviceCollection.AddScoped<Rmv2ModelNodeLoader>();

            serviceCollection.AddScoped<SaveService>();
            serviceCollection.AddScoped<NodeToRmvSaveHelper>();

            serviceCollection.AddScoped<GeometryStrategyProvider>();
            serviceCollection.AddScoped<IGeometryStrategy, NoMeshStrategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw6Strategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw7Strategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw8Strategy>();

            serviceCollection.AddScoped<LodStrategyProvider>();
            serviceCollection.AddScoped<ILodGenerationStrategy, AssetEditorLodGeneration>();
            serviceCollection.AddScoped<ILodGenerationStrategy, Lod0ForAllLodGeneration>();
            serviceCollection.AddScoped<ILodGenerationStrategy, NoLodGeneration>();
            
            //serviceCollection.AddScoped<ILodGenerationStrategy, SimplygonLodGeneration>();

            serviceCollection.AddScoped<MaterialStrategyProvider>();
            serviceCollection.AddScoped<IMaterialStrategy, Warhammer3WsModelStrategy>();
            serviceCollection.AddScoped<IMaterialStrategy, Warhammer2WsModelStrategy>();
            serviceCollection.AddScoped<IMaterialStrategy, PharaohWsModelStrategy>();
            serviceCollection.AddScoped<IMaterialStrategy, NoWsModelStrategy>();

            // Shader
            serviceCollection.AddScoped<CapabilityMaterialFactory>(); 

            // Resolvers - sort of hacks 
            serviceCollection.AddScoped<IDeviceResolver, DeviceResolver>();

            // Components
            RegisterComponents(serviceCollection);

            // Commands
            RegisterCommands(serviceCollection);
        }

        void RegisterComponents(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IComponentInserter, ComponentInserter>();
            RegisterGameComponent<CommandStackRenderer>(serviceCollection);
            RegisterGameComponent<IKeyboardComponent, KeyboardComponent>(serviceCollection);
            RegisterGameComponent<IMouseComponent, MouseComponent>(serviceCollection);

            RegisterGameComponent<FpsComponent>(serviceCollection);
            RegisterGameComponent<ArcBallCamera>(serviceCollection);
            RegisterGameComponent<SceneManager>(serviceCollection);
            RegisterGameComponent<GizmoComponent>(serviceCollection);
            RegisterGameComponent<SelectionManager>(serviceCollection);
            RegisterGameComponent<SelectionComponent>(serviceCollection);
            RegisterGameComponent<RenderEngineComponent>(serviceCollection);
            RegisterGameComponent<GridComponent>(serviceCollection);
            RegisterGameComponent<AnimationsContainerComponent>(serviceCollection);
            RegisterGameComponent<LightControllerComponent>(serviceCollection);

            //serviceCollection.AddScoped<ISceneLightParameters>(x => x.GetRequiredService<LightControllerComponent>()); 
        }

        void RegisterCommands(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<CommandExecutor>();
            serviceCollection.AddScoped<CommandFactory>();

            serviceCollection.AddTransient<ConvertFacesToVertexSelectionCommand>();
            serviceCollection.AddTransient<FaceSelectionCommand>();
            serviceCollection.AddTransient<DuplicateFacesCommand>();
            serviceCollection.AddTransient<VertexSelectionCommand>();
            serviceCollection.AddTransient<ObjectSelectionCommand>();
            serviceCollection.AddTransient<DeleteFaceCommand>();
            serviceCollection.AddTransient<DeleteObjectsCommand>();
            serviceCollection.AddTransient<ReduceMeshCommand>();
            serviceCollection.AddTransient<TransformVertexCommand>();
            serviceCollection.AddTransient<CombineMeshCommand>();
            serviceCollection.AddTransient<CreateAnimatedMeshPoseCommand>();
            serviceCollection.AddTransient<DivideObjectIntoSubmeshesCommand>();
            serviceCollection.AddTransient<DuplicateObjectCommand>();
            serviceCollection.AddTransient<AddObjectsToGroupCommand>();
            serviceCollection.AddTransient<UnGroupObjectsCommand>();
            serviceCollection.AddTransient<GroupObjectsCommand>();
            serviceCollection.AddTransient<GrowMeshCommand>();
            serviceCollection.AddTransient<ObjectSelectionModeCommand>();

            serviceCollection.AddTransient<BoneSelectionCommand>();
            serviceCollection.AddTransient<TransformBoneCommand>();
            serviceCollection.AddTransient<ResetTransformBoneCommand>();
            serviceCollection.AddTransient<PasteWholeTransformBoneCommand>();
            serviceCollection.AddTransient<PasteIntoSelectedBonesTransformBoneCommand>();
            serviceCollection.AddTransient<PasteIntoSelectedBonesInRangeTransformFromClipboardBoneCommand>();
            serviceCollection.AddTransient<PasteIntoSelectedBonesTransformFromClipboardBoneCommand>();
            serviceCollection.AddTransient<PasteWholeInRangeTransformFromClipboardBoneCommand>();
            serviceCollection.AddTransient<PasteWholeTransformFromClipboardBoneCommand>();
            serviceCollection.AddTransient<DuplicateFrameBoneCommand>();
            serviceCollection.AddTransient<DeleteFrameBoneCommand>();
            serviceCollection.AddTransient<InterpolateFramesBoneCommand>();
            serviceCollection.AddTransient<InterpolateFramesSelectedBonesBoneCommand>();
        }


    }
}
