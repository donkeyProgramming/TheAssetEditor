using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Rendering.Shading;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public abstract class IndexedMeshGeometry<VertexType> : IGeometry
        where VertexType : struct, IVertexType
    {
        protected IGeometryGraphicsContext Context;
        protected VertexDeclaration _vertexDeclaration;

        protected VertexType[] _vertexArray;
        protected ushort[] _indexList;

        public Vector3 Pivot { get; set; }

        protected BoundingBox _boundingBox;
        public BoundingBox BoundingBox => _boundingBox;

        Vector3 _meshCenter;
        public Vector3 MeshCenter { get => _meshCenter; protected set => _meshCenter = value; }

        public abstract IGeometry Clone(bool includeMesh = true);

        protected IndexedMeshGeometry(VertexDeclaration vertexDeclaration, IGeometryGraphicsContext context)
        {
            _vertexDeclaration = vertexDeclaration;
            Context = context;
        }

        protected void RebuildIndexBuffer()
        {
            Context.RebuildIndexBuffer(_indexList);
        }

        public void ApplyMesh(IShader effect, GraphicsDevice device)
        {
            if (Context.IndexBuffer == null || Context.VertexBuffer == null)
                return;

            device.Indices = Context.IndexBuffer;
            device.SetVertexBuffer(Context.VertexBuffer);
            foreach (var pass in effect.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Context.IndexBuffer.IndexCount);
            }
        }

        public void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.Indices = Context.IndexBuffer;
            device.SetVertexBuffer(Context.VertexBuffer);
            foreach (var pass in effect.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }
        }

        public int GetIndex(int i)
        {
            return _indexList[i];
        }


        public int GetIndexCount()
        {
            return _indexList.Length;
        }

        public List<ushort> GetIndexBuffer()
        {
            return _indexList.ToList();
        }

        public void SetIndexBuffer(List<ushort> buffer)
        {
            _indexList = buffer.ToArray();
            RebuildIndexBuffer();
        }

        protected void BuildBoundingBox()
        {
            var count = VertexCount();
            if (count == 0)
            {
                _boundingBox = new BoundingBox(-Vector3.One, Vector3.One);
                _meshCenter = Vector3.Zero;
                return;
            }

            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = GetVertexById(i);
            _boundingBox = BoundingBox.CreateFromPoints(points);

            // Update mesh center
            var corners = _boundingBox.GetCorners();
            _meshCenter = Vector3.Zero;
            for (int i = 0; i < corners.Length; i++)
                _meshCenter += corners[i];
            _meshCenter = _meshCenter / corners.Length;
        }

        public virtual void Dispose()
        {
            Context.Dispose();
        }

        public void RemoveFaces(List<int> facesToDelete)
        {
            var newIndexList = new ushort[_indexList.Length - (facesToDelete.Count * 3)];
            var writeIndex = 0;
            for (int i = 0; i < _indexList.Length;)
            {
                if (facesToDelete.Contains(i) == false)
                    newIndexList[writeIndex++] = (_indexList[i++]);
                else
                    i += 3;
            }

            RemoveUnusedVertexes(newIndexList);

        }

        public virtual void RemoveUnusedVertexes(ushort[] newIndexList)
        {
            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            var newVertexList = new List<VertexType>();
            Dictionary<ushort, ushort> remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < _vertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(_vertexArray[i]);
                }
            }

            for (int i = 0; i < newIndexList.Length; i++)
                newIndexList[i] = remappingTable[newIndexList[i]];

            _indexList = newIndexList;
            _vertexArray = newVertexList.ToArray();

            RebuildIndexBuffer();
            RebuildVertexBuffer();
        }

        public IGeometry CloneSubMesh(ushort[] newIndexList)
        {
            var mesh = this.Clone(false) as IndexedMeshGeometry<VertexType>;


            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            var newVertexList = new List<VertexType>();
            Dictionary<ushort, ushort> remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < _vertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(_vertexArray[i]);
                }
            }

            for (int i = 0; i < newIndexList.Length; i++)
                newIndexList[i] = remappingTable[newIndexList[i]];

            mesh._indexList = newIndexList;
            mesh._vertexArray = newVertexList.ToArray();

            mesh.RebuildIndexBuffer();
            mesh.RebuildVertexBuffer();

            return mesh;

        }

        public virtual int VertexCount()
        {
            return _vertexArray.Length;
        }

        public virtual void RebuildVertexBuffer()
        {
            Context.RebuildVertexBuffer(_vertexArray, _vertexDeclaration);
            BuildBoundingBox();
        }

        protected virtual void CopyInto(IndexedMeshGeometry<VertexType> mesh, bool includeMesh = true)
        {
            mesh.Pivot = Pivot;
            mesh.Context = Context.Clone();
            mesh._vertexDeclaration = _vertexDeclaration;
            mesh._boundingBox = BoundingBox;
            mesh._meshCenter = _meshCenter;

            if (includeMesh)
            {
                mesh._indexList = new ushort[_indexList.Length];
                _indexList.CopyTo(mesh._indexList, 0);

                mesh._vertexArray = new VertexType[_vertexArray.Length];
                _vertexArray.CopyTo(mesh._vertexArray, 0);

                mesh.Context.RebuildIndexBuffer(mesh._indexList);
                mesh.Context.RebuildVertexBuffer(mesh._vertexArray, _vertexDeclaration);
            }
        }

        public abstract Vector3 GetVertexById(int id);
        public abstract List<Vector3> GetVertexList();
        public abstract void TransformVertex(int vertexId, Matrix transform);
        public abstract void SetTransformVertex(int vertexId, Matrix transform);

        public abstract List<byte> GetUniqeBlendIndices();
        public abstract void UpdateAnimationIndecies(List<IndexRemapping> remapping);
        public abstract void ChangeVertexType(VertexFormat weighted);
        public abstract void SetVertexWeights(int index, Vector4 newWeights);
        public abstract void SetVertexBlendIndex(int index, Vector4 blendIndex);
    }
}
