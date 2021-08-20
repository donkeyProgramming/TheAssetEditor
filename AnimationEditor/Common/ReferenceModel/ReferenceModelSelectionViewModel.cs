
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFragment;
using CommonControls.PackFileBrowser;
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

        public ReferenceModelSelectionViewModel(PackFileService pf, AssetViewModel data, string headerName, IComponentManager componentManager, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SchemaManager schemaManager)
        {
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
            if (FragAndSlotSelection.FragmentList.SelectedItem != null)
            {
                var view = AnimationFragmentViewModel.CreateFromFragment(_pfs, FragAndSlotSelection.FragmentList.SelectedItem, false);
                TableWindow.Show(view);
            }
        }

        public void ViewMetaData() { }
        public void ViewPersistMetaData() { }


        void MetaDataChanged(AssetViewModel model)
        {
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

            /*
            if (model.MetaData != null)
            {
                var f = MetaDataFileParser.Open(model.MetaData, _schemaManager);
                var props = f.GetItemsOfType("animated_prop");
                foreach(var prop in props)
                {
                    var animatedPropMeta = AnimatedProp.Create(prop);
                    var mesh = _pfs.FindFile(animatedPropMeta.MeshName);

                    var PropPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), "propPlayer"+Guid.NewGuid());

                    SceneLoader loader = new SceneLoader(_componentManager.GetComponent<ResourceLibary>());


                    string skelName = "";
                    var result = loader.Load(mesh, new GroupNode("The dogNode"), PropPlayer, ref skelName);


                    var ske = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, skelName);
                    GameSkeleton s = new GameSkeleton(ske, PropPlayer);

                    var anim = _pfs.FindFile(animatedPropMeta.AnimationName);
                    AnimationFile animFile = AnimationFile.Create(anim);
                    var clip = new AnimationClip(animFile);

                    var rule = new CopyRootTransform(model, animatedPropMeta.BoneId,  animatedPropMeta.Position, animatedPropMeta.Orientation);
                    clip.AnimationRules.Add(rule);

                    //for (int i = 0; i < clip.DynamicFrames.Count; i++)
                    //{
                    //
                    //    var headPos = model.AnimationClip.GetPosition(model.Skeleton, i, 22);
                    //    clip.SetPosition(i, 0, headPos);
                    //}

                    //clip.CopyRootMovementFrom(model.AnimationClip 22);


                    PropPlayer.SetAnimation(clip, s);

                    model.MainNode.AddObject(result);

                    var skeletonSceneNode = new SkeletonNode(_componentManager.GetComponent<ResourceLibary>().Content, new SimpleSkeletonProvider(s));
                    skeletonSceneNode.NodeColour = Color.Yellow;

                    result.AddObject(skeletonSceneNode);

                    var test = new AnimatedPropInstance(result as SceneNode, PropPlayer, model, animatedPropMeta.BoneId, animatedPropMeta.Position, animFile);

                    model.AttachedItems.Add(test);
                }

                // Add stuff

                // Meta files in

                // Persistant first
                // Things to create in scene
                //  - Animated prop
                //  - Effect
                //  - Prop
                //  - Impact pos
                //  - firepos
                // Update the animation
                //  - Transform
                //  - Splice
                //  - 


            }

            */
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
