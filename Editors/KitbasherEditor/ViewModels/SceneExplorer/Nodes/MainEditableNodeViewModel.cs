using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.UiCommands;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels;
using Shared.Core.Events;
using static CommonControls.FilterDialog.FilterUserControl;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class MainEditableNodeViewModel : ObservableObject, ISceneNodeViewModel
    {
        static public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        MainEditableNode _mainNode;

        [ObservableProperty] ObservableCollection<string> _skeletonNameList;
        [ObservableProperty] string _skeletonName;
        [ObservableProperty] ObservableCollection<RenderFormats> _possibleRenderFormats = [RenderFormats.MetalRoughness, RenderFormats.SpecGloss];
        [ObservableProperty] RenderFormats _selectedRenderFormat;

        public MainEditableNodeViewModel(KitbasherRootScene kitbasherRootScene, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            RenderEngineComponent renderEngineComponent, 
            IUiCommandFactory uiCommandFactory)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _renderEngineComponent = renderEngineComponent;
            _uiCommandFactory = uiCommandFactory;
            
            SelectedRenderFormat = _renderEngineComponent.MainRenderFormat;
            SkeletonNameList = _skeletonAnimationLookUpHelper.SkeletonFileNames;
        }

        partial void OnSelectedRenderFormatChanged(RenderFormats value)
        {
            _renderEngineComponent.MainRenderFormat = value;
        }

        partial void OnSkeletonNameChanged(string value)
        {
            var cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(value))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(value);
            _kitbasherRootScene.SetSkeletonFromName(cleanSkeletonName);
        }

        public void Initialize(ISceneNode node)
        {
            _mainNode = node as MainEditableNode;
            if (_mainNode.Model != null)
            {
                SkeletonName = SkeletonNameList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).ToLower() == _mainNode.Model.Header.SkeletonName.ToLower());
            }
        }

        public void CopyTexturesToOutputPack() => _uiCommandFactory.Create<CopyTexturesToPackCommand>().Execute(_mainNode);
        public void DeleteAllMissingTexturesAction() => _uiCommandFactory.Create<DeleteMissingTexturesCommand>().Execute(_mainNode);
        public void Dispose() { }
    }
}
