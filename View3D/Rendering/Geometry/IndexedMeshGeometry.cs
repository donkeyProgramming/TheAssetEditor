using CommonControls.Editors.BoneMapping;
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
    //public abstract class IndexedMeshGeometry<VertexType> : IGeometry
    //    where VertexType : struct, IVertexType
    //{
    //    
    //
    //    public abstract IGeometry Clone(bool includeMesh = true);
    //
    //    protected IndexedMeshGeometry(VertexDeclaration vertexDeclaration, IGeometryGraphicsContext context)
    //    {
    //        _vertexDeclaration = vertexDeclaration;
    //        Context = context;
    //    }
    //
    //    //protected void RebuildIndexBuffer()
    //    //{
    //    //    Context.RebuildIndexBuffer(_indexList);
    //    //}
    //
    //    public abstract void ApplyMesh(IShader effect, GraphicsDevice device);
    //
    //    public abstract void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection);
    //
    //
    //    public abstract int GetIndex(int i);
    //
    //
    //    public abstract int GetIndexCount();
    //    public abstract List<ushort> GetIndexBuffer();
    //
    //    public abstract void SetIndexBuffer(List<ushort> buffer);
    //
    //    abstract void BuildBoundingBox();
    //
    //    public abstract void Dispose();
    //
    //    public abstract void RemoveFaces(List<int> facesToDelete);
    //
    //    public abstract void RemoveUnusedVertexes(ushort[] newIndexList);
    //
    //    public abstract IGeometry CloneSubMesh(ushort[] newIndexList);
    //
    //    public abstract int VertexCount();
    //
    //
    //    public abstract void RebuildVertexBuffer();
    //
    //    protected abstract void CopyInto(IndexedMeshGeometry<VertexType> mesh, bool includeMesh = true);
    //    public abstract Vector3 GetVertexById(int id);
    //    public abstract List<Vector3> GetVertexList();
    //    public abstract void TransformVertex(int vertexId, Matrix transform);
    //    public abstract void SetTransformVertex(int vertexId, Matrix transform);
    //
    //    public abstract List<byte> GetUniqeBlendIndices();
    //    public abstract void UpdateAnimationIndecies(List<IndexRemapping> remapping);
    //    public abstract void ChangeVertexType(VertexFormat weighted);
    //    public abstract void SetVertexWeights(int index, Vector4 newWeights);
    //    public abstract void SetVertexBlendIndex(int index, Vector4 blendIndex);
    //}
}
