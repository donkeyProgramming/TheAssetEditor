using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.SceneNodes;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2
{
    public class WeightedMaterialViewModel : NotifyPropertyChangedImpl
    {
        WeightedMaterial _weightedMaterial;

        public NotifyAttr<string> Filters { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<int> MatrixIndex { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<int> ParentMatrixIndex { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<string> BinaryVertexFormat { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> TransformInfo { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> MaterialId { get; set; } = new NotifyAttr<string>();

        public ObservableCollection<(int Index, string Value)> StringParameters { get; set; }
        public ObservableCollection<(int Index, float Value)> FloatParameters { get; set; }
        public ObservableCollection<(int Index, int Value)> IntParameters { get; set; }
        public ObservableCollection<string> TextureParameters { get; set; }
        public ObservableCollection<string> AttachmentPointParameters { get; set; }
        public ObservableCollection<string> VectorParameters { get; set; }

        public ICommand SetDefaultParentMatrixIndexCommand { get; set; }
        public ICommand SetDefaultMatrixIndexCommand { get; set; }

        public WeightedMaterialViewModel()
        {
            SetDefaultParentMatrixIndexCommand = new RelayCommand(SetDefaultParentMatrix);
            SetDefaultMatrixIndexCommand = new RelayCommand(SetDefaultMatrix);
        }

        public void Initialize(Rmv2MeshNode node)
        {
            if (node.RmvMaterial is not WeightedMaterial castMaterial)
                throw new Exception($"Material is not WeightedMaterial - {node.RmvMaterial.GetType()}");
            _weightedMaterial = castMaterial;

            Filters.Value = _weightedMaterial.Filters;
            MatrixIndex.Value = _weightedMaterial.MatrixIndex;
            ParentMatrixIndex.Value = _weightedMaterial.ParentMatrixIndex;
            BinaryVertexFormat.Value = _weightedMaterial.BinaryVertexFormat.ToString();
            TransformInfo.Value = $"Piv Identity = {_weightedMaterial.OriginalTransform.IsIdentityPivot()} Matrix Identity = {_weightedMaterial.OriginalTransform.IsIdentityMatrices()}";
            MaterialId.Value = _weightedMaterial.MaterialId.ToString();
            
            StringParameters = new ObservableCollection<(int Index, string Value)>(_weightedMaterial.StringParams.Values);
            FloatParameters = new ObservableCollection<(int Index, float Value)>(_weightedMaterial.FloatParams.Values);
            IntParameters = new ObservableCollection<(int, int)>(_weightedMaterial.IntParams.Values);
            TextureParameters = new ObservableCollection<string>(_weightedMaterial.TexturesParams.Select(x => x.TexureType + " - " + x.Path));
            AttachmentPointParameters = new ObservableCollection<string>(_weightedMaterial.AttachmentPointParams.Select(x => x.BoneIndex + " - " + x.Name + " Ident:" + x.Matrix.IsIdentity()));
            VectorParameters = new ObservableCollection<string>(_weightedMaterial.Vec4Params.Values.Select(x => $"[{x.Value.X}] [{x.Value.Y}] [{x.Value.Z}] [{x.Value.W}]"));
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
