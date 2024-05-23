using AnimationMeta.Visualisation.Instances;
using Microsoft.Xna.Framework;
using SharedCore.Misc;
using SharedCore.PackFiles.Models;
using System.Collections.Generic;
using View3D.Animation;
using View3D.Components;
using View3D.SceneNodes;
using static Editors.Shared.Core.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SceneObject : BaseComponent, ISkeletonProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;
        public event ValueChangedDelegate<AnimationClip> AnimationChanged;
        public event ValueChangedDelegate<SceneObject> MeshChanged;
        public event ValueChangedDelegate<SceneObject> MetaDataChanged;

        public void TriggerMeshChanged() => MeshChanged?.Invoke(this);
        public void TriggerSkeletonChanged() => SkeletonChanged?.Invoke(Skeleton);
        public void TriggerMetaDataChanged() => MetaDataChanged?.Invoke(this);
        public void TriggerAnimationChanged() => AnimationChanged?.Invoke(AnimationClip);


        public SceneNode ParentNode { get; set; }
        public SkeletonNode SkeletonSceneNode { get; set; }
        public ISceneNode ModelNode { get; set; }


        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set { _isSelectable = value; SetSelectableState(); } }

        public View3D.Animation.AnimationPlayer Player { get; set; }
        public List<IMetaDataInstance> MetaDataItems { get; set; } = new List<IMetaDataInstance>();

        public SceneNode MainNode { get => ParentNode; }

        public string Description { get; set; }

        public GameSkeleton Skeleton { get; set; }
        public AnimationClip AnimationClip { get; set; }
        public PackFile MetaData { get; set; }
        public PackFile PersistMetaData { get; set; }
        public Matrix Offset { get; set; } = Matrix.Identity;


        // --- UI elements
        public NotifyAttr<string> MeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<AnimationReference> AnimationName { get; set; } = new NotifyAttr<AnimationReference>(null);

        public NotifyAttr<bool> ShowMesh { get; set; }
        public NotifyAttr<bool> ShowSkeleton { get; set; }


        public SceneObject() : base()
        {
            ShowMesh = new NotifyAttr<bool>(true, (x) => SetMeshVisability(x));
            ShowSkeleton = new NotifyAttr<bool>(true, (x) => SkeletonSceneNode.IsVisible = ShowSkeleton.Value);
        }

        public void SetMeshVisability(bool value)
        {
            if (ModelNode == null)
                return;
            ModelNode.IsVisible = value;
        }

        void SetSelectableState()
        {
            if (ModelNode == null)
                return;
            ModelNode.ForeachNodeRecursive((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = IsSelectable;
            });
        }

        public void SetTransform(Matrix matrix)
        {
            if (ModelNode != null)
                ModelNode.ModelMatrix = matrix;
        }

        internal void SelectedBoneIndex(int? boneIndex)
        {
            SkeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        internal void SelectedBoneScale(float scaleMult)
        {
            SkeletonSceneNode.SelectedBoneScaleMult = scaleMult;
        }

        public override void Update(GameTime gameTime)
        {
            ParentNode.ModelMatrix = Matrix.Multiply(Offset, Matrix.Identity);

            var p = Player.CurrentFrame;
            foreach (var item in MetaDataItems)
                item.Update(p);
        }
    }
}
