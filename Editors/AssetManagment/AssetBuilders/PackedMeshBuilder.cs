using System.Linq;
using System.Collections.Generic;
using AssetManagement.GenericFormats.DataStructures.Managed;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.Animation;

namespace AssetManagement.AssetBuilders
{
    public interface IPackedMeshBuilder
    {
        public PackedMesh CreateMesh(RmvModel model);
        public List<PackedMesh> CreateMeshList(RmvFile file, AnimationFile skeletonFile);
    }

    internal class IndexedPackedMeshBuilder : IPackedMeshBuilder
    {
        public virtual PackedMesh CreateMesh(RmvModel model)
        {
            return Rmv2ModelToPackedMesh(model);
        }

        public virtual List<PackedMesh> CreateMeshList(RmvFile file, AnimationFile skeletonFile)
        {
            var outPackedMeshes = new List<PackedMesh>();
            foreach (var model in file.ModelList[0])
            {
                outPackedMeshes.Add(CreateMesh(model));
            }
            return outPackedMeshes;
        }

        public PackedMesh Rmv2ModelToPackedMesh(RmvModel inMmodel)
        {
            var outMesh = new PackedMesh() { Name = inMmodel.Material.ModelName, };

            // TODO: remove linq if it is to slow, and restor original
            //outMesh.Vertices = new ExtPackedCommonVertex[inMmodel.Mesh.VertexList.Length].ToList();
            //outMesh.Indices = new uint[inMmodel.Mesh.IndexList.Length].ToList();

            outMesh.Vertices = inMmodel.Mesh.VertexList.Select(vertex => PackedVertexHelper.CreateExtPackedVertex(vertex)).ToList();
            outMesh.Indices = inMmodel.Mesh.IndexList.Select(index => (uint)index).ToList();

            return outMesh;
        }
    }

    internal class WeightedIndexedPackedMeshBuilder : IndexedPackedMeshBuilder
    {
        readonly AnimationFile _skeletonFile;

        public WeightedIndexedPackedMeshBuilder(AnimationFile skeletonFile)
        {
            _skeletonFile = skeletonFile;
        }

        override public PackedMesh CreateMesh(RmvModel InModel)
        {
            var outMesh = Rmv2ModelToPackedMesh(InModel);

            AddVertexWeights(InModel, outMesh);

            return outMesh;
        }

        public void AddVertexWeights(RmvModel inModel, PackedMesh outMesh)
        {
            var weightcreator = new VertexWeightCreator(inModel, _skeletonFile);
            outMesh.VertexWeights = weightcreator.CreateVertexWeigts();
        }
    }

    public class PackedMeshBuilderFactory
    {
        public static IPackedMeshBuilder GetBuilder(AnimationFile skeletonFile)
        {
            if (skeletonFile == null)
            {
                return new IndexedPackedMeshBuilder();
            }
            else
            {
                return new WeightedIndexedPackedMeshBuilder(skeletonFile); ;

            }
        }
    }
}
