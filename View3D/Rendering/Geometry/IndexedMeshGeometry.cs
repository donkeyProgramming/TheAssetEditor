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
        protected GraphicsDevice _device;
        protected VertexDeclaration _vertexDeclaration;
        protected VertexBuffer _vertexBuffer;
        protected IndexBuffer _indexBuffer;

        protected VertexType[] _vertexArray;


        protected ushort[] _indexList;

        public Vector3 Pivot { get; set; }

        protected BoundingBox _boundingBox;
        public BoundingBox BoundingBox => _boundingBox;

        Vector3 _meshCenter;
        public Vector3 MeshCenter { get => _meshCenter; protected set => _meshCenter = value; }

        public abstract IGeometry Clone();

        protected IndexedMeshGeometry(GraphicsDevice device)
        {
            _device = device;
        }

        protected void CreateIndexFromBuffers()
        {
            _indexBuffer = new IndexBuffer(_device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);
        }

        public void ApplyMesh(IShader effect, GraphicsDevice device)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffer.IndexCount);
            }
        }

        public void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
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

        protected void BuildBoundingBox()
        {
            var count = VertexCount();
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = GetVertexById(i);
            _boundingBox = BoundingBox.CreateFromPoints(points);

            UpdateMeshCenter();
        }

        public virtual void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        public void RemoveFaces(List<int> facesToDelete)
        {
            var newIndexList = new ushort[_indexList.Length - (facesToDelete.Count * 3)];
            var writeIndex = 0;
            for (int i = 0; i < _indexList.Length; )
            {
                if (facesToDelete.Contains(i) == false)
                    newIndexList[writeIndex++] = (_indexList[i++]);
                else
                    i+= 3;
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
            _indexBuffer = new IndexBuffer(_device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);

            _vertexArray = newVertexList.ToArray();
            _vertexBuffer = new VertexBuffer(_device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);

            BuildBoundingBox();
        }

        public virtual int VertexCount()
        {
            return _vertexArray.Length;
        }

        public virtual void RebuildVertexBuffer()
        {
            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();

            _vertexBuffer = new VertexBuffer(_device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);

            //_meshCenter = Vector3.Zero;
            //for (int i = 0; i < VertexCount(); i++)
            //    _meshCenter += GetVertexById(i);
            //
            //_meshCenter = _meshCenter / VertexCount();

            BuildBoundingBox();
        }

        void UpdateMeshCenter()
        {
            var corners = _boundingBox.GetCorners();
            _meshCenter = Vector3.Zero;
            for (int i = 0; i < corners.Length; i++)
                _meshCenter += corners[i];
            _meshCenter = _meshCenter / corners.Length;
        }

        protected virtual void CopyInto(IndexedMeshGeometry<VertexType> mesh)
        {
            mesh.Pivot = Pivot;
            mesh._vertexDeclaration = _vertexDeclaration;
            mesh._boundingBox = BoundingBox;

            mesh._indexList = new ushort[_indexList.Length];
            _indexList.CopyTo(mesh._indexList, 0);

            mesh._indexBuffer = new IndexBuffer(_device, typeof(short), mesh._indexList.Length, BufferUsage.None);
            mesh._indexBuffer.SetData(mesh._indexList);

            mesh._vertexArray = new VertexType[_vertexArray.Length];
            _vertexArray.CopyTo(mesh._vertexArray, 0);

            mesh._vertexBuffer = new VertexBuffer(_device, mesh._vertexDeclaration, mesh._vertexArray.Length, BufferUsage.None);
            mesh._vertexBuffer.SetData(mesh._vertexArray);
        }

        public abstract Vector3 GetVertexById(int id);
        public abstract void UpdateVertexPosition(int vertexId, Vector3 position);

        public abstract List<byte> GetUniqeBlendIndices();
        public abstract void UpdateAnimationIndecies(List<IndexRemapping> remapping);
    }
}
