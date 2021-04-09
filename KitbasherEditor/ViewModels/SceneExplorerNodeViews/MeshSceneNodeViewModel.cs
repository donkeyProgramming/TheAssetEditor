using Common;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping;
using KitbasherEditor.Views.EditorViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TextureEditor.ViewModels;
using View3D.Rendering.Geometry;
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
        public MeshSceneNodeViewModel(Rmv2MeshNode node, PackFileService pf, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = node;
            General = new MeshSceneNodeViewModel_General(_meshNode);
            Animation = new MeshSceneNodeViewModel_Animation(_meshNode, animLookUp);
            Graphics = new MeshSceneNodeViewModel_Graphics(_meshNode, pf);
        }
    }

    public class MeshSceneNodeViewModel_General : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;

        public string ModelName { get { return _meshNode.MeshModel.Header.ModelName; } set { UpdateModelName(value); NotifyPropertyChanged(); } }

        public VertexFormat VertexType { get { return _meshNode.MeshModel.Header.VertextType; } set { ChangeVertexType(value, true); } }

        private void ChangeVertexType(VertexFormat newFormat, bool doMeshUpdate)
        {
            if (doMeshUpdate == false)
            {
                NotifyPropertyChanged(nameof(VertexType));
                return;
            }

            if (!(newFormat == VertexFormat.Weighted || newFormat == VertexFormat.Default))
            {
                MessageBox.Show("Can only swap to weighted or default format.");
                NotifyPropertyChanged(nameof(VertexType));
                return;
            }

            var header = _meshNode.MeshModel.Header;
            header.VertextType = newFormat;
            _meshNode.MeshModel.Header = header;
            _meshNode.Geometry.ChangeVertexType(newFormat);

            NotifyPropertyChanged(nameof(VertexType));
        }

        public IEnumerable<VertexFormat> PossibleVertexTypes { get; set; }

        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }


        bool _drawPivotPoint = false;
        public bool DrawPivotPoint { get { return _drawPivotPoint; } set { SetAndNotify(ref _drawPivotPoint, value); } }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public MeshSceneNodeViewModel_General(Rmv2MeshNode node)
        {
            _meshNode = node;

            PossibleVertexTypes = Enum.GetValues(typeof(VertexFormat)).Cast<VertexFormat>();
            Pivot = new Vector3ViewModel(_meshNode.MeshModel.Header.Transform.Pivot.X, _meshNode.MeshModel.Header.Transform.Pivot.Y, _meshNode.MeshModel.Header.Transform.Pivot.Z);
        }

        void UpdateModelName(string newName)
        {
            var header = _meshNode.MeshModel.Header;
            header.ModelName = newName;
            _meshNode.MeshModel.Header = header;
            _meshNode.Name = newName;
        }
    }

    public class MeshSceneNodeViewModel_Animation : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        SkeletonAnimationLookUpHelper _animLookUp;

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); } }
        public ICommand UseParentSkeletonCommand { get; set; }

        int _linkDirectlyToBoneIndex;
        public int LinkDirectlyToBoneIndex { get { return _linkDirectlyToBoneIndex; } set { SetAndNotify(ref _linkDirectlyToBoneIndex, value); } }


        List<RmvAttachmentPoint> _attachemntPoints;
        public List<RmvAttachmentPoint> AttachmentPoints { get { return _attachemntPoints; } set { SetAndNotify(ref _attachemntPoints, value); } }
        public ICommand UseParentAttachmentPointsCommand { get; set; }

        List<AnimatedBone> _animatedBones;
        public List<AnimatedBone> AnimatedBones { get { return _animatedBones; } set { SetAndNotify(ref _animatedBones, value); } }
        public ICommand OpenBoneRemappingToolCommand { get; set; }

        public MeshSceneNodeViewModel_Animation(Rmv2MeshNode meshNode, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = meshNode;
            _animLookUp = animLookUp;

            SkeletonName = _meshNode.MeshModel.ParentSkeletonName;
            UseParentSkeletonCommand = new RelayCommand(UseParentSkeleton);
            LinkDirectlyToBoneIndex = _meshNode.MeshModel.Header.LinkDirectlyToBoneIndex;
            AttachmentPoints = _meshNode.MeshModel.AttachmentPoints.OrderBy(x => x.BoneIndex).ToList();
            UseParentAttachmentPointsCommand = new RelayCommand(UseParentAttachmentPoints);

            var skeletonFile = _animLookUp.GetSkeletonFileFromName(SkeletonName);
            var bones = _meshNode.Geometry.GetUniqeBlendIndices();
            AnimatedBones = bones.Select(x => new AnimatedBone() { BoneIndex = x, Name = skeletonFile.Bones[x].Name }).OrderBy(x => x.BoneIndex).ToList();
            OpenBoneRemappingToolCommand = new RelayCommand(OpenBoneRemappingTool);
        }


        void UseParentSkeleton() { }

        void UseParentAttachmentPoints() { }
         
        void OpenBoneRemappingTool()
        {
            RemappedAnimatedBoneConfiguration config = new RemappedAnimatedBoneConfiguration();

            var existingSkeletonFile = _animLookUp.GetSkeletonFileFromName(SkeletonName);
            config.MeshSkeletonName = SkeletonName;
            config.MeshBones = AnimatedBone.CreateFromSkeleton(existingSkeletonFile, AnimatedBones.Select(x => x.BoneIndex).ToList());

            var modelNode = _meshNode.GetParentModel();
            var newSkeletonFile = _animLookUp.GetSkeletonFileFromName(modelNode.Model.Header.SkeletonName);
            config.ParnetModelSkeletonName = modelNode.Model.Header.SkeletonName;
            config.ParentModelBones = AnimatedBone.CreateFromSkeleton(newSkeletonFile);

            AnimatedBlendIndexRemappingWindow window = new AnimatedBlendIndexRemappingWindow()
            {
                DataContext = new AnimatedBlendIndexRemappingViewModel(config)
            };

            if (window.ShowDialog() == true)
            {
                List<IndexRemapping> remapping = config.MeshBones.First().BuildRemappingList();
                _meshNode.Geometry.UpdateAnimationIndecies(remapping);
                _meshNode.MeshModel.ParentSkeletonName = config.ParnetModelSkeletonName;
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
                Path = _meshNode.MeshModel.GetTexture(texureType)?.Path;

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

        string _shaderName;
        public string ShaderName { get { return _shaderName; } set { SetAndNotify(ref _shaderName, value); } }


        GroupTypeEnum _materialType;
        public GroupTypeEnum MaterialType { get { return _materialType; } set { SetAndNotify(ref _materialType, value); } }
        public AlphaMode AlphaModeValue { get { return _meshNode.MeshModel.AlphaSettings.Mode; ; } set { UpdateAlphaValue(value); NotifyPropertyChanged(); } }
        public IEnumerable<AlphaMode> PossibleAlphaModes { get; set; } = new List<AlphaMode>() { AlphaMode.Opaque, AlphaMode.Alpha_Test, AlphaMode.Alpha_Blend };
        public string TextureDirectory { get { return _meshNode.MeshModel.Header.TextureDirectory; } set { UpdateTextureDirectory(value); NotifyPropertyChanged(); } }

        public Dictionary<TexureType, TextureViewModel> Textures { get; set; }

        public MeshSceneNodeViewModel_Graphics(Rmv2MeshNode meshNode, PackFileService pf)
        {
            _meshNode = meshNode;
            ShaderName = _meshNode.MeshModel.Header.ShaderParams.ShaderName;
            MaterialType = _meshNode.MeshModel.Header.MaterialId;

            Textures = new Dictionary<TexureType, TextureViewModel>();
            Textures.Add(TexureType.Diffuse, new TextureViewModel(_meshNode, pf,TexureType.Diffuse));
            Textures.Add(TexureType.Specular, new TextureViewModel(_meshNode, pf, TexureType.Specular));
            Textures.Add(TexureType.Normal, new TextureViewModel(_meshNode, pf, TexureType.Normal));
            Textures.Add(TexureType.Mask, new TextureViewModel(_meshNode, pf, TexureType.Mask));
            Textures.Add(TexureType.Gloss, new TextureViewModel(_meshNode, pf, TexureType.Gloss));
        }

        void UpdateAlphaValue(AlphaMode value)
        {
            var alphaSettings = _meshNode.MeshModel.AlphaSettings;
            alphaSettings.Mode = value;
            _meshNode.MeshModel.AlphaSettings = alphaSettings;
        }

        void UpdateTextureDirectory(string newPath)
        {
            var header = _meshNode.MeshModel.Header;
            TextureDirectory = newPath;
            _meshNode.MeshModel.Header = header;
        }
    }
}

