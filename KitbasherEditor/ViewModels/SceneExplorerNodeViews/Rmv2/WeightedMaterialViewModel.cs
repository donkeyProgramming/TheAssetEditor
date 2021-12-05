using Common;
using CommonControls.Common;
using FileTypes.RigidModel.MaterialHeaders;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class WeightedMaterialViewModel : NotifyPropertyChangedImpl
    {
        public NotifyAttr<string> Filters { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<int> MatrixIndex { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<int> ParentMatrixIndex { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<string> BinaryVertexFormat { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> TransformInfo { get; set; } = new NotifyAttr<string>();

        public ObservableCollection<string> StringParameters { get; set; }
        public ObservableCollection<float> FloatParameters { get; set; }
        public ObservableCollection<int> IntParameters { get; set; }
        public ObservableCollection<string> TextureParameters { get; set; }
        public ObservableCollection<string> AttachmentPointParameters { get; set; }
        public ObservableCollection<string> VectorParameters { get; set; }

        public WeightedMaterialViewModel(Rmv2MeshNode node)
        {
            var typedMaterial = node.Material as WeightedMaterial;
            if (typedMaterial == null)
                throw new Exception($"Material is not WeightedMaterial - {node.Material.GetType()}");

            Filters.Value = typedMaterial.Filters;
            MatrixIndex.Value = typedMaterial.MatrixIndex;
            ParentMatrixIndex.Value = typedMaterial.ParentMatrixIndex;
            BinaryVertexFormat.Value = typedMaterial.BinaryVertexFormat.ToString();
            TransformInfo.Value = $"Piv Identity = {typedMaterial.OriginalTransform.IsIdentityPivot()} Matrix Identity = {typedMaterial.OriginalTransform.IsIdentityMatrices()}";

            StringParameters = new ObservableCollection<string>(typedMaterial.StringParams);
            FloatParameters = new ObservableCollection<float>(typedMaterial.FloatParams);
            IntParameters = new ObservableCollection<int>(typedMaterial.IntParams);
            TextureParameters = new ObservableCollection<string>(typedMaterial.TexturesParams.Select(x=>x.TexureType + " - " + x.Path));
            AttachmentPointParameters = new ObservableCollection<string>(typedMaterial.AttachmentPointParams.Select(x => x.BoneIndex+ " - " + x.Name + " Ident:" + x.Matrix.IsIdentity()));
            VectorParameters = new ObservableCollection<string>(typedMaterial.Vec4Params.Select(x => $"[{x.X}] [{x.Y}] [{x.Z}] [{x.W}]"));
        }

    }
}
