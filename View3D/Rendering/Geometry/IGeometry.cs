using CommonControls.Editors.BoneMapping;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Rendering.Shading;

namespace View3D.Rendering.Geometry
{



    public interface IGeometry : IDisposable
    {
        public void ApplyMesh(IShader effect, GraphicsDevice device);
        public void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection);

        public Vector3 GetVertexById(int id);
        public int VertexCount();

        public int GetIndex(int i);
        public int GetIndexCount();
        public List<ushort> GetIndexBuffer();
        public void SetIndexBuffer(List<ushort> buffer);

        BoundingBox BoundingBox { get; }
        Vector3 MeshCenter { get; }

        IGeometry Clone(bool includeMesh = true);
        IGeometry CloneSubMesh(ushort[] newIndexList);

        void RemoveFaces(List<int> facesToDelete);
        List<Vector3> GetVertexList();
        void RemoveUnusedVertexes(ushort[] newIndexList);
        void TransformVertex(int vertexId, Matrix transform);
        void SetTransformVertex(int vertexId, Matrix transform);
        void SetVertexWeights(int index, Vector4 newWeights);
        void SetVertexBlendIndex(int index, Vector4 blendIndex);
        void RebuildVertexBuffer();

        List<byte> GetUniqeBlendIndices();
        void UpdateAnimationIndecies(List<IndexRemapping> remapping);
        void ChangeVertexType(VertexFormat weighted);
        
    }
}
