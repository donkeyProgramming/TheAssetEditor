using _componentManager.ViewModels.MenuBarViews;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<KitbasherView>();
            serviceCollection.AddTransient<KitbasherViewModel>();
            serviceCollection.AddTransient<IEditorViewModel, KitbasherViewModel>();


            serviceCollection.AddSingleton<SceneContainer>();
            serviceCollection.AddSingleton<WpfGame>( x=> x.GetService<SceneContainer>() as WpfGame);

            serviceCollection.AddSingleton<ComponentManagerResolver>();
            serviceCollection.AddSingleton<ComponentInserter>();

            serviceCollection.AddScoped<IDeviceResolver, DeviceResolverComponent>(x => x.GetService<DeviceResolverComponent>());
            

            RegisterGameComponent<DeviceResolverComponent>(serviceCollection);
            RegisterGameComponent<CommandExecutor>(serviceCollection);
            RegisterGameComponent<KeyboardComponent>(serviceCollection);
            RegisterGameComponent<MouseComponent>(serviceCollection);
            RegisterGameComponent<ResourceLibary>(serviceCollection);
            RegisterGameComponent<FpsComponent>(serviceCollection);
            RegisterGameComponent<ArcBallCamera>(serviceCollection);
            RegisterGameComponent<SceneManager>(serviceCollection);
            RegisterGameComponent<SelectionManager>(serviceCollection);
            RegisterGameComponent<GizmoComponent>(serviceCollection);
            RegisterGameComponent<SelectionComponent>(serviceCollection);
            RegisterGameComponent<ObjectEditor>(serviceCollection);
            RegisterGameComponent<FaceEditor>(serviceCollection);
            RegisterGameComponent<FocusSelectableObjectComponent>(serviceCollection);
            RegisterGameComponent<RenderEngineComponent>(serviceCollection);


            RegisterGameComponent<ClearScreenComponent>(serviceCollection);
            RegisterGameComponent<GridComponent>(serviceCollection);
            RegisterGameComponent<AnimationsContainerComponent>(serviceCollection);
            RegisterGameComponent<ViewOnlySelectedComponent>(serviceCollection);
            RegisterGameComponent<LightControllerComponent>(serviceCollection);
            RegisterGameComponent<SkeletonAnimationLookUpHelper>(serviceCollection);
            //RegisterGameComponent<RenderEngineComponent>(serviceCollection);
            //RegisterGameComponent<RenderEngineComponent>(serviceCollection);

            serviceCollection.AddScoped<KitbashSceneCreator>();
            

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
