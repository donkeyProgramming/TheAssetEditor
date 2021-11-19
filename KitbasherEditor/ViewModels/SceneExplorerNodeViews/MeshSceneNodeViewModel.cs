using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.RigidModel.MaterialHeaders;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TextureEditor.ViewModels;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MeshSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        public MeshSceneNodeViewModel_General General { get; set; }
        public MeshSceneNodeViewModel_Animation Animation { get; set; }
        public MeshSceneNodeViewModel_Graphics Graphics { get; set; }

        Rmv2MeshNode _meshNode;
        public MeshSceneNodeViewModel(Rmv2MeshNode node, PackFileService pfs, SkeletonAnimationLookUpHelper animLookUp, IComponentManager componentManager)
        {
            _meshNode = node;
            General = new MeshSceneNodeViewModel_General(_meshNode, componentManager);

            if (node.Material is WeightedMaterial)
            {

                Animation = new MeshSceneNodeViewModel_Animation(pfs, _meshNode, animLookUp, componentManager);
                Graphics = new MeshSceneNodeViewModel_Graphics(_meshNode, pfs, componentManager);
            }
            else
            {
                Animation = null;
                Graphics = null;
            }
        }

        public void Dispose()
        {

        }
    }

    public class MeshSceneNodeViewModel_General : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        IComponentManager _componentManager;

        public string ModelName { get { return _meshNode.Material.ModelName; } set { _meshNode.Material.ModelName = value; NotifyPropertyChanged(); } }

        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }


        public bool DrawBoundingBox { get { return _meshNode.DisplayBoundingBox; } set { _meshNode.DisplayBoundingBox = value; NotifyPropertyChanged(); } }
        public bool DrawPivotPoint { get { return _meshNode.DisplayPivotPoint; } set { _meshNode.DisplayPivotPoint = value; NotifyPropertyChanged();} }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public ICommand CopyPivotToAllMeshesCommand { get; set; }

        public MeshSceneNodeViewModel_General(Rmv2MeshNode node, IComponentManager componentManager)
        {
            _meshNode = node;
            _meshNode.Name = _meshNode.Material.ModelName;
            _componentManager = componentManager;
            Pivot = new Vector3ViewModel(_meshNode.Material.PivotPoint);
            Pivot.OnValueChanged += Pivot_OnValueChanged;
            CopyPivotToAllMeshesCommand = new RelayCommand(CopyPivotToAllMeshes);
        }

        private void Pivot_OnValueChanged(Vector3ViewModel newValue)
        {
            _meshNode.UpdatePivotPoint(new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value));
           
        } 

        void CopyPivotToAllMeshes()
        {
            var newPiv = new Vector3((float)Pivot.X.Value, (float)Pivot.Y.Value, (float)Pivot.Z.Value);

            var root = _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode();
            var allMeshes = root.GetMeshesInLod(0, false);
            foreach (var mesh in allMeshes)
                mesh.UpdatePivotPoint(newPiv);
        }
    }

    public class MeshSceneNodeViewModel_Animation : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        SkeletonAnimationLookUpHelper _animLookUp;
        PackFileService _pfs;
        IComponentManager _componentManager;

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); } }

        int _linkDirectlyToBoneIndex;
        public int LinkDirectlyToBoneIndex { get { return _linkDirectlyToBoneIndex; } set { SetAndNotify(ref _linkDirectlyToBoneIndex, value); } }


        List<AnimatedBone> _animatedBones;
        public List<AnimatedBone> AnimatedBones { get { return _animatedBones; } set { SetAndNotify(ref _animatedBones, value); } }

        public FilterCollection<AnimatedBone> ModelBoneList { get; set; } = new FilterCollection<AnimatedBone>(null); 

        public MeshSceneNodeViewModel_Animation(PackFileService pfs, Rmv2MeshNode meshNode, SkeletonAnimationLookUpHelper animLookUp, IComponentManager componentManager)
        {
            _pfs = pfs;
            _meshNode = meshNode;
            _animLookUp = animLookUp;
            _componentManager = componentManager; 

            SkeletonName = _meshNode.Geometry.ParentSkeletonName;
            
            //LinkDirectlyToBoneIndex = (_meshNode.Material as WeightedMaterial).MatrixIndex;

            var skeletonFile = _animLookUp.GetSkeletonFileFromName(_pfs, SkeletonName);
            var bones = _meshNode.Geometry.GetUniqeBlendIndices();

            // Make sure the bones are valid, mapping can cause issues! 
            if (bones.Count != 0)
            {
                var activeBonesMin = bones.Min(x => x);
                var activeBonesMax = bones.Max(x => x);
                var skeletonBonesMax = skeletonFile.Bones.Max(x => x.Id);

                bool hasValidBoneMapping = activeBonesMin >= 0 && skeletonBonesMax >= activeBonesMax;
                if (!hasValidBoneMapping)
                    MessageBox.Show("Mesh an invalid bones, this might cause issues. Its a result of an invalid re-rigging");

                var boneList = AnimatedBoneHelper.CreateFlatSkeletonList(skeletonFile);

                if (skeletonFile != null && hasValidBoneMapping)
                {
                    AnimatedBones = boneList
                        .OrderBy(x => x.BoneIndex.Value)
                        .ToList();
                }

            }

            var existingSkeletonMeshNode = _meshNode.GetParentModel();
            var existingSkeltonName = existingSkeletonMeshNode.Model.Header.SkeletonName;
            var existingSkeletonFile = _animLookUp.GetSkeletonFileFromName(_pfs, existingSkeltonName);
            if(existingSkeletonFile != null)
                ModelBoneList.UpdatePossibleValues(AnimatedBoneHelper.CreateFlatSkeletonList(existingSkeletonFile), new AnimatedBone(-1, "none"));
            ModelBoneList.SelectedItemChanged += ModelBoneList_SelectedItemChanged;
            ModelBoneList.SearchFilter = (value, rx) => { return rx.Match(value.Name.Value).Success; };
            ModelBoneList.SelectedItem = ModelBoneList.PossibleValues.FirstOrDefault(x => x.Name.Value == _meshNode.AttachmentPointName);
         
        }

        private void ModelBoneList_SelectedItemChanged(AnimatedBone newValue)
        {
            MainEditableNode mainNode = _meshNode.GetParentModel() as MainEditableNode;
            if (mainNode == null)
                return;

            if (newValue != null && newValue.BoneIndex.Value != -1)
            {
                _meshNode.AttachmentPointName = newValue.Name.Value;
                _meshNode.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(mainNode.Skeleton.AnimationProvider, newValue.BoneIndex.Value);
            }
            else
            {
                _meshNode.AttachmentPointName = null;
                _meshNode.AttachmentBoneResolver = null;
            }
        }
    }

    public class MeshSceneNodeViewModel_Graphics : NotifyPropertyChangedImpl
    {
        public class TextureViewModel : NotifyPropertyChangedImpl
        {
            PackFileService _packfileService;
            Rmv2MeshNode _meshNode;
            TexureType _texureType;
            bool _useTexture = true;
            public bool UseTexture { get { return _useTexture; } set { SetAndNotify(ref _useTexture, value); UpdateUseTexture(value); } }

            string _path;
            public string Path { get { return _path; } set { SetAndNotify(ref _path, value); } }


            public ICommand PreviewCommand { get; set; }
            public ICommand BrowseCommand { get; set; }
            public ICommand RemoveCommand { get; set; }
 

            public TextureViewModel(Rmv2MeshNode meshNode, PackFileService packfileService,  TexureType texureType)
            {
                _packfileService = packfileService;
                _meshNode = meshNode;
                _texureType = texureType;
                Path = _meshNode.Material.GetTexture(texureType)?.Path;

                PreviewCommand = new RelayCommand(() => TexturePreviewController.CreateVindow(Path, _packfileService));
                BrowseCommand = new RelayCommand(BrowseTexture);
                RemoveCommand = new RelayCommand(RemoveTexture);
            }

            void BrowseTexture() 
            {
                using (var browser = new PackFileBrowserWindow(_packfileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".dds", ".png", });
                    if (browser.ShowDialog() == true && browser.SelectedFile != null)
                    {
                        try
                        {
                            Path = _packfileService.GetFullPath(browser.SelectedFile);
                            _meshNode.UpdateTexture(Path, _texureType);
                        }
                        catch 
                        {
                            UpdateUseTexture(false);
                        }
                    }
                }
            }
            void RemoveTexture() 
            {
            }

            public void UpdateUseTexture(bool value)
            {
                _meshNode.UseTexture(_texureType, value);
            }
        }

        Rmv2MeshNode _meshNode;
        IComponentManager _componentManager;

        string _shaderName;
        public string ShaderName { get { return _shaderName; } set { SetAndNotify(ref _shaderName, value); } }

        public ModelMaterialEnum MaterialType { get { return _meshNode.RmvModel_depricated.CommonHeader.ModelTypeFlag; } set { UpdateGroupType(value); NotifyPropertyChanged(); } }
        public AlphaMode AlphaModeValue { get { return _meshNode.Material.AlphaMode; } set { _meshNode.Material.AlphaMode = value; NotifyPropertyChanged(); } }
        public IEnumerable<AlphaMode> PossibleAlphaModes { get; set; } = new List<AlphaMode>() { AlphaMode.Opaque, AlphaMode.Alpha_Test, AlphaMode.Alpha_Blend };
        public string TextureDirectory { get { return (_meshNode.Material as WeightedMaterial).TextureDirectory; } set { (_meshNode.Material as WeightedMaterial).TextureDirectory = value; NotifyPropertyChanged(); } }
        public bool ReduceMeshOnLodGeneration { get { return _meshNode.ReduceMeshOnLodGeneration; } set { _meshNode.ReduceMeshOnLodGeneration = value; NotifyPropertyChanged(); } }
        public IEnumerable<ModelMaterialEnum> PossibleMaterialTypes { get; set; }

        public Dictionary<TexureType, TextureViewModel> Textures { get; set; }

        public UiVertexFormat VertexType { get { return _meshNode.Geometry.VertexFormat; } set { ChangeVertexType(value); } }

        void ChangeVertexType(UiVertexFormat newFormat)
        {
            var mainNode = _componentManager.GetComponent<IEditableMeshResolver>();
            var skeletonName = mainNode.GeEditableMeshRootNode()?.Skeleton.Name;
            _meshNode.Geometry.ChangeVertexType(newFormat, skeletonName);
            NotifyPropertyChanged(nameof(VertexType));
        }

        public IEnumerable<UiVertexFormat> PossibleVertexTypes { get; set; }

        public MeshSceneNodeViewModel_Graphics(Rmv2MeshNode meshNode, PackFileService pf, IComponentManager componentManager)
        {
            _componentManager = componentManager;
            _meshNode = meshNode;
            ShaderName = _meshNode.RmvModel_depricated.CommonHeader.ShaderParams.ShaderName;
            PossibleMaterialTypes = Enum.GetValues(typeof(ModelMaterialEnum)).Cast<ModelMaterialEnum>();
            PossibleVertexTypes = new UiVertexFormat[] { UiVertexFormat.Static, UiVertexFormat.Weighted, UiVertexFormat.Cinematic };

            Textures = new Dictionary<TexureType, TextureViewModel>();
            Textures.Add(TexureType.Diffuse, new TextureViewModel(_meshNode, pf,TexureType.Diffuse));
            Textures.Add(TexureType.Specular, new TextureViewModel(_meshNode, pf, TexureType.Specular));
            Textures.Add(TexureType.Normal, new TextureViewModel(_meshNode, pf, TexureType.Normal));
            Textures.Add(TexureType.Mask, new TextureViewModel(_meshNode, pf, TexureType.Mask));
            Textures.Add(TexureType.Gloss, new TextureViewModel(_meshNode, pf, TexureType.Gloss));
        }

        void UpdateGroupType(ModelMaterialEnum value)
        {
            throw new NotImplementedException("TODO");
            //var header = _meshNode.RmvModel_depricated.Header;
            //header.ModelTypeFlag = value;
            //_meshNode.RmvModel_depricated.Header = header;
        }
    }
}

