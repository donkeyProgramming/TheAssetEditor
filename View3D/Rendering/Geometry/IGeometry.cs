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

        BoundingBox BoundingBox { get; }
        Vector3 MeshCenter { get; }

        IGeometry Clone(bool includeMesh = true);

        void RemoveFaces(List<int> facesToDelete);
        void RemoveUnusedVertexes(ushort[] newIndexList);
        void UpdateVertexPosition(int vertexId, Vector3 position);
        void RebuildVertexBuffer();

        List<byte> GetUniqeBlendIndices();
        void UpdateAnimationIndecies(List<IndexRemapping> remapping);
    }

    public class IndexRemapping
    {
        public IndexRemapping(byte originalValue, byte newValue)
        {
            OriginalValue = originalValue;
            NewValue = newValue;
        }

        public byte OriginalValue { get; set; }
        public byte NewValue { get; set; }
    }
}
