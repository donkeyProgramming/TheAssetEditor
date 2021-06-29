using Common;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping;
using KitbasherEditor.ViewModels.BmiEditor;
using KitbasherEditor.Views.EditorViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TextureEditor.ViewModels;
using View3D.Animation;
using View3D.Commands.Object;
using View3D.Components.Component;
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
        public MeshSceneNodeViewModel(Rmv2MeshNode node, PackFileService pfs, SkeletonAnimationLookUpHelper animLookUp, IComponentManager componentManager)
        {
            _meshNode = node;
            General = new MeshSceneNodeViewModel_General(_meshNode);
            Animation = new MeshSceneNodeViewModel_Animation(pfs, _meshNode, animLookUp, componentManager);
            Graphics = new MeshSceneNodeViewModel_Graphics(_meshNode, pfs);
        }

        public void Dispose()
        {

        }
    }

    public class MeshSceneNodeViewModel_General : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;

        public string ModelName { get { return _meshNode.MeshModel.Header.ModelName; } set { UpdateModelName(value); NotifyPropertyChanged(); } }

        

        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }


        public bool DrawBoundingBox { get { return _meshNode.DisplayBoundingBox; } set { _meshNode.DisplayBoundingBox = value; NotifyPropertyChanged(); } }
        public bool DrawPivotPoint { get { return _meshNode.DisplayPivotPoint; } set { _meshNode.DisplayPivotPoint = value; NotifyPropertyChanged();} }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public MeshSceneNodeViewModel_General(Rmv2MeshNode node)
        {
            _meshNode = node;
            Pivot = new Vector3ViewModel(_meshNode.MeshModel.Header.Transform.Pivot.X, _meshNode.MeshModel.Header.Transform.Pivot.Y, _meshNode.MeshModel.Header.Transform.Pivot.Z);
            Pivot.OnValueChanged += Pivot_OnValueChanged;
        }

        private void Pivot_OnValueChanged(Vector3ViewModel newValue)
        {
            var header = _meshNode.MeshModel.Header;
            var transform = header.Transform;

            transform.Pivot = new Filetypes.RigidModel.Transforms.RmvVector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            
            header.Transform = transform;
            _meshNode.MeshModel.Header = header;
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
        PackFileService _pfs;
        IComponentManager _componentManager;

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); } }

        int _linkDirectlyToBoneIndex;
        public int LinkDirectlyToBoneIndex { get { return _linkDirectlyToBoneIndex; } set { SetAndNotify(ref _linkDirectlyToBoneIndex, value); } }


        List<RmvAttachmentPoint> _attachemntPoints;
        public List<RmvAttachmentPoint> AttachmentPoints { get { return _attachemntPoints; } set { SetAndNotify(ref _attachemntPoints, value); } }

        List<AnimatedBone> _animatedBones;
        public List<AnimatedBone> AnimatedBones { get { return _animatedBones; } set { SetAndNotify(ref _animatedBones, value); } }
        public ICommand OpenBoneRemappingToolCommand { get; set; }

        public MeshSceneNodeViewModel_Animation(PackFileService pfs, Rmv2MeshNode meshNode, SkeletonAnimationLookUpHelper animLookUp, IComponentManager componentManager)
        {
            _pfs = pfs;
            _meshNode = meshNode;
            _animLookUp = animLookUp;
            _componentManager = componentManager; 

            SkeletonName = _meshNode.MeshModel.ParentSkeletonName;
            LinkDirectlyToBoneIndex = _meshNode.MeshModel.Header.LinkDirectlyToBoneIndex;
            AttachmentPoints = _meshNode.MeshModel.AttachmentPoints.OrderBy(x => x.BoneIndex).ToList();

            var skeletonFile = _animLookUp.GetSkeletonFileFromName(_pfs, SkeletonName);
            var bones = _meshNode.Geometry.GetUniqeBlendIndices();
            AnimatedBones = bones.Select(x => new AnimatedBone() { BoneIndex = x, Name = skeletonFile.Bones[x].Name }).OrderBy(x => x.BoneIndex).ToList();
            OpenBoneRemappingToolCommand = new RelayCommand(OpenBoneRemappingTool);
        }

        void OpenBoneRemappingTool()
        {
            var targetSkeletonName = _meshNode.MeshModel.ParentSkeletonName;

            var existingSkeletonMeshNode = _meshNode.GetParentModel();
            var existingSkeltonName = existingSkeletonMeshNode.Model.Header.SkeletonName;

            RemappedAnimatedBoneConfiguration config = new RemappedAnimatedBoneConfiguration();

            var existingSkeletonFile = _animLookUp.GetSkeletonFileFromName(_pfs, targetSkeletonName);
            config.MeshSkeletonName = targetSkeletonName;
            config.MeshBones = AnimatedBone.CreateFromSkeleton(existingSkeletonFile, AnimatedBones.Select(x => x.BoneIndex).ToList());
            

            var newSkeletonFile = _animLookUp.GetSkeletonFileFromName(_pfs, existingSkeltonName);
            config.ParnetModelSkeletonName = existingSkeltonName;
            config.ParentModelBones = AnimatedBone.CreateFromSkeleton(newSkeletonFile);



            AnimatedBlendIndexRemappingWindow window = new AnimatedBlendIndexRemappingWindow()
            {
                DataContext = new AnimatedBlendIndexRemappingViewModel(config)
            };

            if (window.ShowDialog() == true)
            {
                var remapping = config.MeshBones.First().BuildRemappingList();
                _componentManager.GetComponent<CommandExecutor>().ExecuteCommand(new RemapBoneIndexesCommand(_meshNode, remapping, config.ParnetModelSkeletonName, config.MoveMeshToFit, new GameSkeleton(existingSkeletonFile, null), new GameSkeleton(newSkeletonFile, null)));
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

        public GroupTypeEnum MaterialType { get { return _meshNode.MeshModel.Header.MaterialId; } set { UpdateGroupType(value); NotifyPropertyChanged(); } }
        public AlphaMode AlphaModeValue { get { return _meshNode.MeshModel.AlphaSettings.Mode; ; } set { UpdateAlphaValue(value); NotifyPropertyChanged(); } }
        public IEnumerable<AlphaMode> PossibleAlphaModes { get; set; } = new List<AlphaMode>() { AlphaMode.Opaque, AlphaMode.Alpha_Test, AlphaMode.Alpha_Blend };
        public string TextureDirectory { get { return _meshNode.MeshModel.Header.TextureDirectory; } set { UpdateTextureDirectory(value); NotifyPropertyChanged(); } }
        public bool ReduceMeshOnLodGeneration { get { return _meshNode.ReduceMeshOnLodGeneration; } set { _meshNode.ReduceMeshOnLodGeneration = value; NotifyPropertyChanged(); } }
        public IEnumerable<GroupTypeEnum> PossibleMaterialTypes { get; set; }

        public Dictionary<TexureType, TextureViewModel> Textures { get; set; }

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

            if (newFormat == VertexFormat.Weighted)
                MaterialType = GroupTypeEnum.weighted;
            else if (newFormat == VertexFormat.Default)
                MaterialType = GroupTypeEnum.default_type;
            else 
                throw new Exception("Unknown vertex format, can not set grouptype");

            var header = _meshNode.MeshModel.Header;
            header.VertextType = newFormat;
            _meshNode.MeshModel.Header = header;
            _meshNode.Geometry.ChangeVertexType(newFormat);



            NotifyPropertyChanged(nameof(VertexType));
        }

        public IEnumerable<VertexFormat> PossibleVertexTypes { get; set; }

        public MeshSceneNodeViewModel_Graphics(Rmv2MeshNode meshNode, PackFileService pf)
        {
            _meshNode = meshNode;
            ShaderName = _meshNode.MeshModel.Header.ShaderParams.ShaderName;
            PossibleMaterialTypes = Enum.GetValues(typeof(GroupTypeEnum)).Cast<GroupTypeEnum>();
            PossibleVertexTypes = Enum.GetValues(typeof(VertexFormat)).Cast<VertexFormat>();

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

        void UpdateGroupType(GroupTypeEnum value)
        {
            var header = _meshNode.MeshModel.Header;
            header.MaterialId = value;
            _meshNode.MeshModel.Header = header;
        }
    }
}

