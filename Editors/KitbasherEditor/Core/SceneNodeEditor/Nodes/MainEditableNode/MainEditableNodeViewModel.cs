using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.UiCommands;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views.EditorViews;
using Shared.Core.Events;
using Shared.Ui.Common.DataTemplates;
using static CommonControls.FilterDialog.FilterUserControl;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class MainEditableNodeViewModel : ObservableObject, ISceneNodeEditor, IViewProvider<MainEditableNodeView>
    {
        static public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        MainEditableNode _mainNode;

        [ObservableProperty] ObservableCollection<string> _skeletonNameList;
        [ObservableProperty] string? _skeletonName;


        public MainEditableNodeViewModel(KitbasherRootScene kitbasherRootScene, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            RenderEngineComponent renderEngineComponent, 
            IUiCommandFactory uiCommandFactory)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _renderEngineComponent = renderEngineComponent;
            _uiCommandFactory = uiCommandFactory;
            
            SkeletonNameList = _skeletonAnimationLookUpHelper.GetAllSkeletonFileNames();
        }


        partial void OnSkeletonNameChanged(string? value)
        {
            var cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(value))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(value);
            _kitbasherRootScene.SetSkeletonFromName(cleanSkeletonName);
        }

        public void Initialize(ISceneNode node)
        {
            if (node is MainEditableNode mainEditableNode)
            {
                _mainNode = mainEditableNode;
                if (_mainNode.SkeletonNode != null)
                    SkeletonName = SkeletonNameList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).ToLower() == _mainNode.SkeletonNode.Skeleton.SkeletonName.ToLower());
            }
            else
            {
                throw new Exception($"{node} is not of type {nameof(MainEditableNode)}");
            }
        }

        public void CopyTexturesToOutputPack() => _uiCommandFactory.Create<CopyTexturesToPackCommand>().Execute(_mainNode);
        public void Dispose() { }
    }
}
