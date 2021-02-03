using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    class Rmv2Geometry : IGeometry
    {
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private VertexDeclaration _vertexDeclaration;

        VertexPositionNormalTextureCustom[] _vertexArray;
        ushort[] _indexList;

        public int WeightCount { get; set; } = 0;
        public Vector3 Pivot { get; set; }

        public Rmv2Geometry(RmvSubModel modelPart, GraphicsDevice device)
        {
            Pivot = new Vector3(modelPart.Header.Transform.Pivot.X, modelPart.Header.Transform.Pivot.Y, modelPart.Header.Transform.Pivot.Z);

            _vertexDeclaration = VertexPositionNormalTextureCustom.VertexDeclaration;
            _vertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            _indexList = (ushort[])modelPart.Mesh._indexList.Clone();

            for (int i = 0; i < modelPart.Mesh.VertexList.Length; i++)
            {
                var vertex = modelPart.Mesh.VertexList[i];
                _vertexArray[i].Position = new Vector4(vertex.Postition.X, vertex.Postition.Y, vertex.Postition.Z, 1);

                _vertexArray[i].Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                _vertexArray[i].BiNormal = new Vector3(vertex.BiNormal.X, vertex.BiNormal.Y, vertex.BiNormal.Z);
                _vertexArray[i].Tangent = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                _vertexArray[i].TextureCoordinate = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                _vertexArray[i].BlendIndices = Vector4.Zero;
                _vertexArray[i].BlendWeights = Vector4.Zero;

                if (modelPart.Header.VertextType == VertexFormat.Cinematic)
                {
                    _vertexArray[i].BlendIndices.X = vertex.BoneIndex[0];
                    _vertexArray[i].BlendIndices.Y = vertex.BoneIndex[1];
                    _vertexArray[i].BlendIndices.Z = vertex.BoneIndex[2];
                    _vertexArray[i].BlendIndices.W = vertex.BoneIndex[3];

                    _vertexArray[i].BlendWeights.X = vertex.BoneWeight[0];
                    _vertexArray[i].BlendWeights.Y = vertex.BoneWeight[1];
                    _vertexArray[i].BlendWeights.Z = vertex.BoneWeight[2];
                    _vertexArray[i].BlendWeights.W = vertex.BoneWeight[3];

                    WeightCount = 4;
                }
                else if (modelPart.Header.VertextType == VertexFormat.Weighted)
                {
                    _vertexArray[i].BlendIndices.X = vertex.BoneIndex[0];
                    _vertexArray[i].BlendWeights.X = vertex.BoneWeight[0];
                    WeightCount = 1;
                }
                else
                {
                    throw new Exception("Unknown vertex format");
                }
            }

            CreateModelFromBuffers(device);
        }

        void CreateModelFromBuffers(GraphicsDevice device)
        {
            _indexBuffer = new IndexBuffer(device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);

            _vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);
        }

        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffer.IndexCount);
            }
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }

        }

        public void Dispose()
        {
           // throw new NotImplementedException();
        }

        public float? Intersect(Ray ray, Matrix modelMatrix)
        {
            //face = null;

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _indexList.Length; i += 3)
            {
                var index0 = _indexList[i + 0];
                var index1 = _indexList[i + 1];
                var index2 = _indexList[i + 2];

                var vert0 = new Vector3(_vertexArray[index0].Position.X, _vertexArray[index0].Position.Y, _vertexArray[index0].Position.Z);
                var vert1 = new Vector3(_vertexArray[index1].Position.X, _vertexArray[index1].Position.Y, _vertexArray[index1].Position.Z);
                var vert2 = new Vector3(_vertexArray[index2].Position.X, _vertexArray[index2].Position.Y, _vertexArray[index2].Position.Z);

                var res = IntersectionMath.MollerTrumboreIntersection(ray, vert0, vert1, vert2, out var intersectionPoint);
                if (res)
                {
                    var dist = intersectionPoint;
                    if (dist < bestDistance)
                    {
                        faceIndex = i;
                        bestDistance = dist.Value;
                    }
                }
            }

            if(faceIndex != -1)
                return bestDistance;
            return null;
        }

        public bool IntersectFace(Ray ray, Matrix modelMatrix, out int? face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _indexList.Length; i += 3)
            {
                var index0 = _indexList[i + 0];
                var index1 = _indexList[i + 1];
                var index2 = _indexList[i + 2];

                var vert0 = new Vector3(_vertexArray[index0].Position.X, _vertexArray[index0].Position.Y, _vertexArray[index0].Position.Z);
                var vert1 = new Vector3(_vertexArray[index1].Position.X, _vertexArray[index1].Position.Y, _vertexArray[index1].Position.Z);
                var vert2 = new Vector3(_vertexArray[index2].Position.X, _vertexArray[index2].Position.Y, _vertexArray[index2].Position.Z);

                var res = IntersectionMath.MollerTrumboreIntersection(ray, vert0, vert1, vert2, out var intersectionPoint);
                if (res)
                {
                    var dist = intersectionPoint;
                    if (dist < bestDistance)
                    {
                        faceIndex = i;
                        bestDistance = dist.Value;
                    }
                }
            }

            if (faceIndex == -1)
                return false;

            face = (faceIndex);
            return true;
        }

        public Vector3 GetVertex(int index)
        {
            throw new NotImplementedException();
        }

        public int VertexCount()
        {
            throw new NotImplementedException();
        }
    }
}
