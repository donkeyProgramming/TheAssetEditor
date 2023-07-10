using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.SceneNodes;
using View3D.Services;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MainEditableNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        MainEditableNode _mainNode;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        PackFileService _pfs;
        IComponentManager _componentManager;

        public ObservableCollection<string> SkeletonNameList { get; set; }

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); UpdateSkeletonName(); } }
        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        public ObservableCollection<RmvVersionEnum> PossibleOutputFormats { get; set; } = new ObservableCollection<RmvVersionEnum>() { RmvVersionEnum.RMV2_V6, RmvVersionEnum.RMV2_V7, RmvVersionEnum.RMV2_V8 };

        RmvVersionEnum _selectedOutputFormat;
        public RmvVersionEnum SelectedOutputFormat { get => _selectedOutputFormat; set { SetAndNotify(ref _selectedOutputFormat, value); _mainNode.SelectedOutputFormat = value; } }


        public ObservableCollection<RenderFormats> PossibleRenderFormats { get; set; } = new ObservableCollection<RenderFormats>() { RenderFormats.MetalRoughness, RenderFormats.SpecGloss };

        RenderFormats _selectedRenderFormat;
        public RenderFormats SelectedRenderFormat { get => _selectedRenderFormat; set { SetAndNotify(ref _selectedRenderFormat, value); _componentManager.GetComponent<RenderEngineComponent>().MainRenderFormat = _selectedRenderFormat; } }

        public TextureFileEditorServiceViewModel TextureFileEditorServiceViewModel { get; set; }

        public ObservableCollection<LodGroupNodeViewModel> LodNodes { get; set; } = new ObservableCollection<LodGroupNodeViewModel>();

        public MainEditableNodeViewModel(MainEditableNode mainNode, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService pfs, IComponentManager componentManager)
        {
            _mainNode = mainNode;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _pfs = pfs;
            _componentManager = componentManager;
            _selectedRenderFormat = _componentManager.GetComponent<RenderEngineComponent>().MainRenderFormat;

            SkeletonNameList = _skeletonAnimationLookUpHelper.SkeletonFileNames;
            if (_mainNode.Model != null)
            {
                SkeletonName = SkeletonNameList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).ToLower() == _mainNode.Model.Header.SkeletonName.ToLower());
                UpdateSkeletonName();
            }

            SelectedOutputFormat = _mainNode.SelectedOutputFormat;
            TextureFileEditorServiceViewModel = new TextureFileEditorServiceViewModel(mainNode, pfs);

            UpdateLodInformationAction();
        }

        void UpdateSkeletonName()
        {

            string cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(SkeletonName))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(SkeletonName);
            _mainNode.SetSkeletonFromName(cleanSkeletonName);
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

        public void UpdateLodInformationAction()
        {
            LodNodes.Clear();
            foreach (var lodNode in _mainNode.GetLodNodes())
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, _componentManager));
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
