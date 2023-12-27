using CommonControls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Commands;
using View3D.Commands.Bone;
using View3D.Commands.Bone.Clipboard;
using View3D.Commands.Face;
using View3D.Commands.Object;
using View3D.Commands.Vertex;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Services.SceneSaving;
using View3D.Services.SceneSaving.Geometry;
using View3D.Services.SceneSaving.Geometry.Strategies;
using View3D.Services.SceneSaving.Lod;
using View3D.Services.SceneSaving.Lod.Strategies;
using View3D.Services.SceneSaving.Material.Strategies;
using View3D.Services.SceneSaving.WsModel;
using View3D.Utility;

namespace View3D
{
    public class View3D_DependencyContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Graphics scene
            serviceCollection.AddScoped<GameWorld>();
            serviceCollection.AddScoped<WpfGame>(x => x.GetService<GameWorld>());
            serviceCollection.AddScoped<IGeometryGraphicsContextFactory, GeometryGraphicsContextFactory>();

            // Services
            serviceCollection.AddScoped<ViewOnlySelectedService>();
            serviceCollection.AddScoped<FocusSelectableObjectService>();
            serviceCollection.AddScoped<ComplexMeshLoader>();
            serviceCollection.AddScoped<WsModelGeneratorService>();
            serviceCollection.AddScoped<FaceEditor>();
            serviceCollection.AddScoped<ObjectEditor>();
            serviceCollection.AddScoped<Rmv2ModelNodeLoader>();

            serviceCollection.AddScoped<SaveService>();
            serviceCollection.AddScoped<SceneSaverService>();

            serviceCollection.AddScoped<GeometryStrategyProvider>();
            serviceCollection.AddScoped<IGeometryStrategy, NoMeshStrategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw6Strategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw7Strategy>();
            serviceCollection.AddScoped<IGeometryStrategy, Rmw8Strategy>();

            serviceCollection.AddScoped<LodStrategyProvider>();
            serviceCollection.AddScoped<ILodGenerationStrategy, DefaultLodGeneration>();
            serviceCollection.AddScoped<ILodGenerationStrategy, Lod0ForAllLodGeneration>();
            serviceCollection.AddScoped<ILodGenerationStrategy, SimplygonLodGeneration>();

            serviceCollection.AddScoped<MaterialStrategyProvider>();
            serviceCollection.AddScoped<IMaterialStrategy, Warhammer3WsModelStrategy>();
            serviceCollection.AddScoped<IMaterialStrategy, Warhammer2WsModelStrategy>();
            serviceCollection.AddScoped<IMaterialStrategy, NoWsModelStrategy>();
            

            // Resolvers - sort of hacks 
            serviceCollection.AddScoped<IDeviceResolver, DeviceResolverComponent>(x => x.GetService<DeviceResolverComponent>());

            // Components
            RegisterComponents(serviceCollection);

            // Commands
            RegisterCommands(serviceCollection);
        }

        void RegisterComponents(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IComponentInserter, ComponentInserter>();
            RegisterGameComponent<DeviceResolverComponent>(serviceCollection);
            RegisterGameComponent<CommandStackRenderer>(serviceCollection);
            RegisterGameComponent<KeyboardComponent>(serviceCollection);
            RegisterGameComponent<MouseComponent>(serviceCollection);
            RegisterGameComponent<ResourceLibary>(serviceCollection);
            RegisterGameComponent<FpsComponent>(serviceCollection);
            RegisterGameComponent<ArcBallCamera>(serviceCollection);
            RegisterGameComponent<SceneManager>(serviceCollection);
            RegisterGameComponent<GizmoComponent>(serviceCollection);
            RegisterGameComponent<SelectionManager>(serviceCollection);
            RegisterGameComponent<SelectionComponent>(serviceCollection);
            RegisterGameComponent<RenderEngineComponent>(serviceCollection);
            RegisterGameComponent<ClearScreenComponent>(serviceCollection);
            RegisterGameComponent<GridComponent>(serviceCollection);
            RegisterGameComponent<AnimationsContainerComponent>(serviceCollection);
            RegisterGameComponent<LightControllerComponent>(serviceCollection);
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
            serviceCollection.AddTransient<PinMeshToVertexCommand>();
            serviceCollection.AddTransient<RemapBoneIndexesCommand>();
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
        protected void RegisterGameComponent<T>(IServiceCollection serviceCollection) where T : class, IGameComponent
        {
            serviceCollection.AddScoped<T>();
            serviceCollection.AddScoped<IGameComponent, T>(x => x.GetService<T>());
        }
    }


}
