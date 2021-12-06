using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using System.Linq;
using View3D.Scene;

namespace AnimationEditor.SuperView
{
    public class Editor
    {
        SceneContainer _scene;
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        AnimationPlayerViewModel _player;
        SchemaManager _schemaManager;
        IToolFactory _toolFactory;

        public ObservableCollection<ReferenceModelSelectionViewModel> Items { get; set; } = new ObservableCollection<ReferenceModelSelectionViewModel>();

        public Editor(IToolFactory toolFactory, SceneContainer scene, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, AnimationPlayerViewModel player, SchemaManager schemaManager)
        {
            _toolFactory = toolFactory;
            _scene = scene;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _player = player;
            _schemaManager = schemaManager;
        }

        public void Create(AnimationToolInput input)
        {
            var asset = _scene.AddCompnent(new AssetViewModel(_pfs, "Item 0", Color.Black, _scene));
            _player.RegisterAsset(asset);
            var viewModel = new ReferenceModelSelectionViewModel(_toolFactory, _pfs, asset, "Item 0:", _scene, _skeletonHelper, _schemaManager);
            viewModel.AllowMetaData.Value = true;

            if(input.Mesh != null)
                viewModel.Data.SetMesh(input.Mesh);
            if (input.Animation != null)
                viewModel.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));

            if (input.FragmentName != null)
            {
                viewModel.FragAndSlotSelection.FragmentList.SelectedItem = viewModel.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FileName == input.FragmentName);

                if (input.AnimationSlot != null)
                {
                    viewModel.FragAndSlotSelection.FragmentSlotList.SelectedItem = viewModel.FragAndSlotSelection.FragmentSlotList.PossibleValues.FirstOrDefault(x => x.Slot.Id == input.AnimationSlot.Id);
                }
            }

            Items.Add(viewModel);
        }

        public void Refresh()
        {
            foreach (var item in Items)
                item.Refresh();

            foreach (var item in Items)
                item.Data.ReApplyMeta();
        }

    }
}
