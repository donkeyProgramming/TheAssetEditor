using _componentManager.ViewModels.MenuBarViews;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Events;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using View3D.Commands;
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
using View3D.Scene;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer
    {
        public static void RegisterNotificationHandler<TNotification, TImplementation>(IServiceCollection serviceCollection)
            where TNotification : INotification 
            where TImplementation : class, INotificationHandler<TNotification>
        {
            serviceCollection.AddScoped<INotificationHandler<TNotification>, TImplementation>(x =>x.GetRequiredService<TImplementation>());
        }

        public static void Register(IServiceCollection serviceCollection)
        {

            RegisterNotificationHandler<SceneInitializedEvent, KitbasherViewModel>(serviceCollection);
            RegisterNotificationHandler<FileSavedEvent, KitbasherViewModel>(serviceCollection);
            RegisterNotificationHandler<CommandStackChangedEvent, KitbasherViewModel>(serviceCollection);

            RegisterNotificationHandler<CommandStackChangedEvent, CommandStackRenderer>(serviceCollection);
            RegisterNotificationHandler<CommandStackUndoEvent, CommandStackRenderer>(serviceCollection);


            RegisterNotificationHandler<SelectionChangedEvent, GizmoComponent>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, MenuBarViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, VertexDebuggerViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, SceneExplorerViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, TransformToolViewModel>(serviceCollection);

            serviceCollection.AddScoped<VertexDebuggerViewModel>();


            serviceCollection.AddScoped<KitbasherView>();
            serviceCollection.AddScoped<KitbasherViewModel>();
            serviceCollection.AddScoped<IEditorViewModel, KitbasherViewModel>();


            serviceCollection.AddScoped<SceneContainer>();
            serviceCollection.AddScoped<WpfGame>( x=> x.GetService<SceneContainer>() as WpfGame);

            serviceCollection.AddScoped<ComponentManagerResolver>();
            serviceCollection.AddScoped<ComponentInserter>();

            serviceCollection.AddScoped<IDeviceResolver, DeviceResolverComponent>(x => x.GetService<DeviceResolverComponent>());
            serviceCollection.AddScoped<CommandExecutor>();
            
            serviceCollection.AddScoped<SceneNodeViewFactory>();
       
            

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


            serviceCollection.AddScoped<ViewOnlySelectedService>();
            serviceCollection.AddScoped<FocusSelectableObjectService>();
            serviceCollection.AddScoped<KitbashSceneCreator>();
            serviceCollection.AddScoped<FaceEditor>();
            serviceCollection.AddScoped<ObjectEditor>();
            serviceCollection.AddScoped<SceneExplorerViewModel>();
            serviceCollection.AddScoped<TransformToolViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();

            serviceCollection.AddScoped<GizmoActions>();
            serviceCollection.AddScoped<VisibilityHandler>();

            serviceCollection.AddScoped<GeneralActions>();
            serviceCollection.AddScoped<ToolActions>();


            serviceCollection.AddScoped<WindowKeyboard>();

            serviceCollection.AddScoped<KitbashSceneCreator>();
            serviceCollection.AddScoped<IGeometryGraphicsContextFactory, GeometryGraphicsContextFactory>();


            serviceCollection.AddScoped<AnimationControllerViewModel>();

            serviceCollection.AddScoped<ActiveFileResolver>();

            serviceCollection.AddScoped<SceneSaverService>();
            serviceCollection.AddScoped<WsModelGeneratorService>();

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


            // Add all ICommand as AddTransient
            // Add all Comonents as scoped


        }



        public static void RegisterGameComponent<T>(IServiceCollection serviceCollection) where T : class, IGameComponent
        {
            serviceCollection.AddScoped<T>();
            serviceCollection.AddScoped<IGameComponent, T>(x => x.GetService<T>());

        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterFileTool<KitbasherViewModel, KitbasherView>(new ExtentionToTool(EditorEnums.Kitbash_Editor, new[] { ".rigid_model_v2", ".wsmodel.rigid_model_v2" }/*, new[] { ".wsmodel", ".variantmeshdefinition" }*/));
        }


        public class ComponentInserter
        {
            private readonly ComponentManagerResolver _componentManagerResolver;
            private readonly IEnumerable<IGameComponent> _components;

            public ComponentInserter(ComponentManagerResolver componentManagerResolver, IEnumerable<IGameComponent> components)
            {
                _componentManagerResolver = componentManagerResolver;
                _components = components;
            }

            public void Execute()
            {
                foreach (var component in _components)
                    _componentManagerResolver.ComponentManager.AddComponent(component);
            }
        }

    
    }


    public static class KitbashEditor_Debug
    {
        public static void CreateSlayerHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
        public static void CreateSlayerBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\body\dwf_slayers_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
        public static void CreateLoremasterHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_loremaster_of_hoeth\hef_loremaster_of_hoeth_head_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreatePaladin(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\brt\brt_paladin\head\brt_paladin_head_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreateSkavenSlaveHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu17\skv\skv_clan_rats\head\skv_clan_rats_head_04.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreatePrincessBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes/wh_variantmodels/hu1b/hef/hef_princess/hef_princess_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreateOgre(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu13\ogr\ogr_maneater\ogr_maneater_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
    }
}
