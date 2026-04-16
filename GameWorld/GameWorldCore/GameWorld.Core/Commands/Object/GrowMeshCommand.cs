using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace GameWorld.Core.Commands.Object
{
    public class GrowMeshCommand : ICommand
    {
        Rmv2MeshNode _node;
        float _factor;
        List<int> _bonesAffectedScale;
        GameSkeleton _gameSkeleton;

        MeshObject _backup;

        public void Configure(GameSkeleton skeleton, Rmv2MeshNode node, float factor, List<int> bonesAffectedScale)
        {
            _node = node;
            _factor = factor;
            _gameSkeleton = skeleton;
            _bonesAffectedScale = bonesAffectedScale;
        }


        public string HintText { get => "Grow mesh"; }
        public bool IsMutation { get => true; }

        public void Execute()
        {
            _backup = _node.Geometry.Clone();

            var mesh = _node.Geometry as MeshObject;
            var vertexCount = mesh.VertexCount();

            var normals = new Vector3[vertexCount];
            var tolerance = 0.0001f;

            for (var i = 0; i < vertexCount; i++)
            {
                normals[i] = mesh.GetVertexExtented(i).Normal;

                // Find all vertexes with the same position
                for (var j = 0; j < vertexCount; j++)
                {
                    if (i == j)
                        continue;

                    var distance = (mesh.GetVertexById(i) - mesh.GetVertexById(j)).LengthSquared();
                    if (distance < tolerance)
                    {
                        normals[i] += mesh.GetVertexExtented(j).Normal;
                    }
                }

                normals[i] = Vector3.Normalize(normals[i]);
            }

            for (var i = 0; i < mesh.VertexCount(); i++)
            {
                var vertex = mesh.GetVertexExtented(i);
                var bones = vertex.GetBoneIndexs();
                var weights = vertex.GetBoneWeights();

                float boneWeight = 0;

                for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    if (_bonesAffectedScale.Contains(bones[boneIndex]))
                        boneWeight += weights[boneIndex];
                }

                var offsetMatrix = Matrix.CreateTranslation(normals[i] * _factor * boneWeight);
                mesh.TransformVertex(i, offsetMatrix);
            }

            mesh.RebuildVertexBuffer();
        }

        public void Undo()
        {
            _node.Geometry = _backup;
        }
    }
}
