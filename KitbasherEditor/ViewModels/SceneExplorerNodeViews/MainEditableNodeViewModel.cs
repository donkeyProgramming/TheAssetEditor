using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommonControls.Common;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.SceneNodes;
using View3D.Services;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MainEditableNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        MainEditableNode _mainNode;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly PackFileService _pfs;
        private readonly RenderEngineComponent _renderEngineComponent;

        public ObservableCollection<string> SkeletonNameList { get; set; }

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); UpdateSkeletonName(); } }
        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        public ObservableCollection<RenderFormats> PossibleRenderFormats { get; set; } = new ObservableCollection<RenderFormats>() { RenderFormats.MetalRoughness, RenderFormats.SpecGloss };

        RenderFormats _selectedRenderFormat;
        public RenderFormats SelectedRenderFormat { get => _selectedRenderFormat; set { SetAndNotify(ref _selectedRenderFormat, value); _renderEngineComponent.MainRenderFormat = _selectedRenderFormat; } }

        public TextureFileEditorServiceViewModel TextureFileEditorServiceViewModel { get; set; }

        public MainEditableNodeViewModel(KitbasherRootScene kitbasherRootScene, MainEditableNode mainNode, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService pfs, RenderEngineComponent renderEngineComponent)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _mainNode = mainNode;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _pfs = pfs;
            _renderEngineComponent = renderEngineComponent;
            _selectedRenderFormat = _renderEngineComponent.MainRenderFormat;

            SkeletonNameList = _skeletonAnimationLookUpHelper.SkeletonFileNames;
            if (_mainNode.Model != null)
            {
                SkeletonName = SkeletonNameList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).ToLower() == _mainNode.Model.Header.SkeletonName.ToLower());
            }

            TextureFileEditorServiceViewModel = new TextureFileEditorServiceViewModel(mainNode);
        }

        void UpdateSkeletonName()
        {
            var cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(SkeletonName))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(SkeletonName);
            _kitbasherRootScene.SetSkeletonFromName(cleanSkeletonName);
        }

        public void CopyTexturesToOutputPack()
        {
            var meshes = _mainNode.GetMeshesInLod(0, false);
            var materials = meshes.Select(x => x.Material);
            var allTextures = materials.SelectMany(x => x.GetAllTextures());
            var distinctTextures = allTextures.DistinctBy(x => x.Path);

            foreach (var tex in distinctTextures)
            {
                var file = _pfs.FindFile(tex.Path);
                if (file != null)
                {
                    var sourcePackContainer = _pfs.GetPackFileContainer(file);
                    _pfs.CopyFileFromOtherPackFile(sourcePackContainer, tex.Path, _pfs.GetEditablePack());
                }
            }
        }

        public void DeleteAllMissingTexturesAction()
        {
            var meshes = _mainNode.GetMeshesInLod(0, false);
            foreach (var mesh in meshes)
            {
                var resolver = new MissingTextureResolver();
                resolver.DeleteMissingTextures(mesh, _pfs);
            }
        }

        public void Dispose()
        {
            _skeletonAnimationLookUpHelper = null;
            _mainNode = null;
        }
    }
}
