
using Common;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Table;
using Filetypes.RigidModel;
using FileTypes.DB;
using FileTypes.MetaData;
using FileTypes.MetaData.Instances;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Animation.AnimationChange;
using View3D.Animation.MetaData;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace AnimationEditor.Common.ReferenceModel
{
    public class ReferenceModelSelectionViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<ReferenceModelSelectionViewModel>();
        SchemaManager _schemaManager;
        PackFileService _pfs;
        IComponentManager _componentManager;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        IToolFactory _toolFactory;

        // Header
        string _headerName;
        public string HeaderName { get => _headerName; set => SetAndNotify(ref _headerName, value); }

        string _subHeaderName = "";
        public string SubHeaderName { get => _subHeaderName; set => SetAndNotify(ref _subHeaderName, value); }

        AssetViewModel _data;
        public AssetViewModel Data { get => _data; set => SetAndNotify(ref _data, value); }

        public SelectMeshViewModel MeshViewModel { get; set; }
        public SelectAnimationViewModel AnimViewModel { get; set; }
        public SkeletonPreviewViewModel SkeletonInformation { get; set; }
        public SelectMetaViewModel MetaFileInformation { get; set; }
        public SelectFragAndSlotViewModel FragAndSlotSelection { get; set; }

       


        // Visability
        bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                SetAndNotify(ref _isVisible, value);
                Data.ShowMesh.Value = value;
                Data.ShowSkeleton.Value = value;
            }
        }

        public NotifyAttr<bool> IsControlVisible { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<bool> AllowMetaData { get; set; } = new NotifyAttr<bool>(false);
        public ReferenceModelSelectionViewModel(IToolFactory toolFactory, PackFileService pf, AssetViewModel data, string headerName, IComponentManager componentManager, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SchemaManager schemaManager)
        {
            _toolFactory = toolFactory;
            _pfs = pf;
            HeaderName = headerName;
            _componentManager = componentManager;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _schemaManager = schemaManager;
            Data = data;

            MeshViewModel = new SelectMeshViewModel(_pfs, Data);
            AnimViewModel = new SelectAnimationViewModel(Data, _pfs, skeletonAnimationLookUpHelper);
            SkeletonInformation = new SkeletonPreviewViewModel(Data);
            MetaFileInformation = new SelectMetaViewModel(Data, _pfs);
            FragAndSlotSelection = new SelectFragAndSlotViewModel(_pfs, skeletonAnimationLookUpHelper, Data, MetaFileInformation);

            // Data.PropertyChanged += Data_PropertyChanged;
            Data.AnimationChanged += Data_AnimationChanged;
            Data.SkeletonChanged += Data_SkeletonChanged;
            Data.MetaDataChanged += MetaDataChanged;
        }

        private void Database_ContainerUpdated(PackFileContainer container)
        {
          //  throw new NotImplementedException();
        }

        private void Data_SkeletonChanged(GameSkeleton newValue)
        {
            Data_PropertyChanged(null, null);
        }

        private void Data_AnimationChanged(AnimationClip newValue)
        {
            Data_PropertyChanged(null, null);
        }

        private void Data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SubHeaderName = "";

            if (Data.Skeleton != null)
                SubHeaderName = Data.Skeleton.SkeletonName;

            if (Data.AnimationClip != null)
                SubHeaderName += " - " + Data.AnimationName.Value;
        }

        public void BrowseMesh()
        {
            MeshViewModel.BrowseMesh();
        }

        public void ViewFragment()
        {
            FragAndSlotSelection.PreviewSelectedSlot();
        }

        public void ViewSelectedMeta() 
        {
            var fullFileName = _pfs.GetFullPath(_data.MetaData);
            var viewModel = _toolFactory.GetToolViewModelFromFileName(fullFileName);
            viewModel.MainFile = _data.MetaData;
            var window = _toolFactory.CreateToolAsWindow(viewModel);
            window.Show();
        }

        public void ViewSelectedPersistMeta()
        {
            var fullFileName = _pfs.GetFullPath(_data.PersistMetaData);
            var viewModel = _toolFactory.GetToolViewModelFromFileName(fullFileName);
            viewModel.MainFile = _data.PersistMetaData;
            var window = _toolFactory.CreateToolAsWindow(viewModel);
            window.Width = 800;
            window.Height = 450;
            window.Title = "Persistent meta file - " + fullFileName;

            window.Show();
        }


        void MetaDataChanged(AssetViewModel model)
        {
            if (AllowMetaData.Value == false)
                return;

            foreach (var item in model.MetaDataItems)
                item.CleanUp();
            model.MetaDataItems.Clear();
            model.Player.AnimationRules.Clear();

            var persist = MetaDataFileParser.Open(model.PersistMetaData, _schemaManager);
            var meta = MetaDataFileParser.Open(model.MetaData, _schemaManager);

            if (persist == null && meta == null)
                return;

            var fatory = new MetaDataFactory(model.MainNode , _componentManager, model, model.Player, FragAndSlotSelection.FragmentList.SelectedItem);
            model.MetaDataItems = fatory.Create(persist, meta);

        }

        public void Refresh()
        {
            MetaFileInformation.Refresh();
        }
    }

   /* public interface IAttachedItem
    {
        View3D.Animation.AnimationPlayer Player { get; }
        void Update(float t);
    }

    public class AnimatedPropInstance : IAttachedItem
    {
        //AnimationPlayer Player { get; set; }
        SkeletonBoneAnimationResolver Resolver;
        SceneNode _modelNode;
        Vector3 _offset;
        AnimationFile _animFile;
        AssetViewModel _parent;

        public AnimatedPropInstance(SceneNode modelNode, View3D.Animation.AnimationPlayer player, AssetViewModel parent, int boneIndex, Vector3 offset, AnimationFile animFile)
        {
            Resolver = new SkeletonBoneAnimationResolver(parent, boneIndex);
            Player = player;
            _parent = parent;
            _offset = offset;
            _animFile = animFile;
            _modelNode = modelNode;



            var parentClip = _parent.AnimationClip;


        }

        public View3D.Animation.AnimationPlayer Player { get; private set; }

        public void Cleanup()
        { }

        public void Update(float t)
        {



           // _modelNode.ModelMatrix = /*Matrix.CreateTranslation(-_offset) * Resolver.GetTransformIfAnimating();
        }
    }*/
}
