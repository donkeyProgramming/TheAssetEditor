using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using MoreLinq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.SceneNodes;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MainEditableNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        MainEditableNode _mainNode;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AnimationControllerViewModel _animationControllerViewModel;
        PackFileService _pfs;
        IComponentManager _componentManager;

        public ObservableCollection<string> SkeletonNameList { get; set; }

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); UpdateSkeletonName(); } }
        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        public ObservableCollection<RmvVersionEnum> PossibleOutputFormats { get; set; } = new ObservableCollection<RmvVersionEnum>();

        RmvVersionEnum _selectedOutputFormat;
        public RmvVersionEnum SelectedOutputFormat { get => _selectedOutputFormat; set { SetAndNotify(ref _selectedOutputFormat, value); _mainNode.SelectedOutputFormat = value; } }


        public ObservableCollection<RenderFormats> PossibleRenderFormats { get; set; } = new ObservableCollection<RenderFormats>() { RenderFormats.MetalRoughness, RenderFormats.SpecGloss };

        RenderFormats _selectedRenderFormat;
        public RenderFormats SelectedRenderFormat { get => _selectedRenderFormat; set { SetAndNotify(ref _selectedRenderFormat, value); _componentManager.GetComponent<RenderEngineComponent>().MainRenderFormat = _selectedRenderFormat; } }


        public MainEditableNodeViewModel(MainEditableNode mainNode, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AnimationControllerViewModel animationControllerViewModel, PackFileService pfs, IComponentManager componentManager)
        {
            _mainNode = mainNode;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _animationControllerViewModel = animationControllerViewModel;
            _pfs = pfs;
            _componentManager = componentManager;
            _selectedRenderFormat = _componentManager.GetComponent<RenderEngineComponent>().MainRenderFormat;

            SkeletonNameList = _skeletonAnimationLookUpHelper.SkeletonFileNames;
            if (_mainNode.Model != null)
            {
                SkeletonName = SkeletonNameList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).ToLower() == _mainNode.Model.Header.SkeletonName.ToLower());
                UpdateSkeletonName();
            }

            SetCurrentOuputFormat(_mainNode.SelectedOutputFormat);
        }

        public void SetCurrentOuputFormat(RmvVersionEnum format)
        {
            SelectedOutputFormat = format;

            PossibleOutputFormats.Clear();
            if (SelectedOutputFormat == RmvVersionEnum.RMV2_V6)
            {
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V6);
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V7);
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V8);
            }
            else if (SelectedOutputFormat == RmvVersionEnum.RMV2_V7 || SelectedOutputFormat == RmvVersionEnum.RMV2_V8)
            {
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V6);
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V7);
                PossibleOutputFormats.Add(RmvVersionEnum.RMV2_V8);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void UpdateSkeletonName()
        {
            string cleanName = "";
            if (!string.IsNullOrWhiteSpace(SkeletonName))
                cleanName = Path.GetFileNameWithoutExtension(SkeletonName);

            if (_mainNode.Model != null)
                SetSkeletonName(_mainNode, cleanName);

            _animationControllerViewModel.SetActiveSkeleton(cleanName);   
        }

        void SetSkeletonName(MainEditableNode node, string skeletonName)
        {
            var header = node.Model.Header;
            header.SkeletonName = skeletonName;
            node.Model.Header = header;
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
                if (file  != null)
                {
                    var sourcePackContainer = _pfs.GetPackFileContainer(file);
                    _pfs.CopyFileFromOtherPackFile(sourcePackContainer, tex.Path, _pfs.GetEditablePack());
                }
            }
        }

        public void Dispose()
        {
            _skeletonAnimationLookUpHelper = null;
            _mainNode = null;
        }
    }
}
