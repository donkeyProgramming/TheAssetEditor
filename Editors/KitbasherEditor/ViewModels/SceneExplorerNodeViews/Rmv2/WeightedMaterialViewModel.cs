using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.SceneNodes;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class WeightedMaterialViewModel : NotifyPropertyChangedImpl
    {
        private readonly WeightedMaterial _weightedMaterial;

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

        public ICommand SetDefaultParentMatrixIndexCommand { get; set; }
        public ICommand SetDefaultMatrixIndexCommand { get; set; }

        public WeightedMaterialViewModel(Rmv2MeshNode node)
        {
            var castMaterial = node.Material as WeightedMaterial;
            if (castMaterial == null)
                throw new Exception($"Material is not WeightedMaterial - {node.Material.GetType()}");
            _weightedMaterial = castMaterial;

            Filters.Value = _weightedMaterial.Filters;
            MatrixIndex.Value = _weightedMaterial.MatrixIndex;
            ParentMatrixIndex.Value = _weightedMaterial.ParentMatrixIndex;
            BinaryVertexFormat.Value = _weightedMaterial.BinaryVertexFormat.ToString();
            TransformInfo.Value = $"Piv Identity = {_weightedMaterial.OriginalTransform.IsIdentityPivot()} Matrix Identity = {_weightedMaterial.OriginalTransform.IsIdentityMatrices()}";

            StringParameters = new ObservableCollection<string>(_weightedMaterial.StringParams);
            FloatParameters = new ObservableCollection<float>(_weightedMaterial.FloatParams);
            IntParameters = new ObservableCollection<int>(_weightedMaterial.IntParams);
            TextureParameters = new ObservableCollection<string>(_weightedMaterial.TexturesParams.Select(x => x.TexureType + " - " + x.Path));
            AttachmentPointParameters = new ObservableCollection<string>(_weightedMaterial.AttachmentPointParams.Select(x => x.BoneIndex + " - " + x.Name + " Ident:" + x.Matrix.IsIdentity()));
            VectorParameters = new ObservableCollection<string>(_weightedMaterial.Vec4Params.Select(x => $"[{x.X}] [{x.Y}] [{x.Z}] [{x.W}]"));

            SetDefaultParentMatrixIndexCommand = new RelayCommand(SetDefaultParentMatrix);
            SetDefaultMatrixIndexCommand = new RelayCommand(SetDefaultMatrix);
        }

        void SetDefaultMatrix()
        {
            _weightedMaterial.MatrixIndex = -1;
            MatrixIndex.Value = _weightedMaterial.MatrixIndex;
        }

        void SetDefaultParentMatrix()
        {
            _weightedMaterial.ParentMatrixIndex = -1;
            ParentMatrixIndex.Value = _weightedMaterial.MatrixIndex;
        }

    }
}
