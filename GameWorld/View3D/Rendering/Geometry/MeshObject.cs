﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Editors.BoneMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Rendering.Shading;

namespace View3D.Rendering.Geometry
{
    public class MeshObject
    {
        protected IGraphicsCardGeometry Context;
        public VertexPositionNormalTextureCustom[] VertexArray; // Vector3 for pos at some point
        public ushort[] IndexArray;

        public BoundingBox BoundingBox { get; private set; }
        public Vector3 MeshCenter { get; private set; }

        public int WeightCount { get; private set; } = 0;
        public UiVertexFormat VertexFormat { get; private set; } = UiVertexFormat.Unknown;
        public string ParentSkeletonName { get; set; }

        public MeshObject(IGraphicsCardGeometry context, string skeletonName)
        {
            ParentSkeletonName = skeletonName;
            Context = context;
        }

        public MeshObject Clone(bool includeMesh = true)
        {
            var mesh = new MeshObject(Context, ParentSkeletonName);

            mesh.Context = Context.Clone();
            mesh.BoundingBox = BoundingBox;
            mesh.MeshCenter = MeshCenter;
            mesh.ParentSkeletonName = ParentSkeletonName;
            mesh.WeightCount = WeightCount;
            mesh.VertexFormat = VertexFormat;

            if (includeMesh)
            {
                mesh.IndexArray = new ushort[IndexArray.Length];
                IndexArray.CopyTo(mesh.IndexArray, 0);

                mesh.VertexArray = new VertexPositionNormalTextureCustom[VertexArray.Length];
                VertexArray.CopyTo(mesh.VertexArray, 0);

                mesh.Context.RebuildIndexBuffer(mesh.IndexArray);
                mesh.Context.RebuildVertexBuffer(mesh.VertexArray, VertexPositionNormalTextureCustom.VertexDeclaration);
            }

            return mesh;
        }


        public Vector3 GetVertexById(int id)
        {
            return new Vector3(VertexArray[id].Position.X, VertexArray[id].Position.Y, VertexArray[id].Position.Z);
        }

        public List<Vector3> GetVertexList()
        {
            var vertCount = VertexArray.Length;
            List<Vector3> output = new List<Vector3>(vertCount);
            for (int i = 0; i < vertCount; i++)
                output.Add(GetVertexById(i));
            return output;
        }

        public void TransformVertex(int vertexId, Matrix transform)
        {
            VertexArray[vertexId].Position = Vector4.Transform(VertexArray[vertexId].Position, transform);

            VertexArray[vertexId].Position.X = VertexArray[vertexId].Position.X / VertexArray[vertexId].Position.W;
            VertexArray[vertexId].Position.Y = VertexArray[vertexId].Position.Y / VertexArray[vertexId].Position.W;
            VertexArray[vertexId].Position.Z = VertexArray[vertexId].Position.Z / VertexArray[vertexId].Position.W;
            VertexArray[vertexId].Position.W = 1;

            var normalMatrix = Matrix.Transpose(Matrix.Invert(transform));

            VertexArray[vertexId].Normal = Vector3.TransformNormal(VertexArray[vertexId].Normal, normalMatrix);
            VertexArray[vertexId].BiNormal = Vector3.TransformNormal(VertexArray[vertexId].BiNormal, normalMatrix);
            VertexArray[vertexId].Tangent = Vector3.TransformNormal(VertexArray[vertexId].Tangent, normalMatrix);

            VertexArray[vertexId].Normal.Normalize();
            VertexArray[vertexId].BiNormal.Normalize();
            VertexArray[vertexId].Tangent.Normalize();
        }

        public void SetVertexWeights(int index, Vector4 newWeights)
        {
            if (VertexFormat == UiVertexFormat.Weighted)
            {
                newWeights.X = newWeights.X + newWeights.Z;
                newWeights.Y = newWeights.Y + newWeights.W;
            }

            VertexArray[index].BlendWeights = newWeights;
        }

        public void SetVertexBlendIndex(int index, Vector4 blendIndex)
        {
            if (VertexFormat == UiVertexFormat.Weighted)
            {
                blendIndex.Z = 0;
                blendIndex.W = 0;
            }

            VertexArray[index].BlendIndices = blendIndex;
        }

        public List<byte> GetUniqeBlendIndices()
        {
            if (WeightCount == 0)
            {
                return new List<byte>();
            }
            else if (WeightCount == 2 || WeightCount == 4)
            {
                var output = new List<byte>();
                for (int i = 0; i < VertexArray.Count(); i++)
                {
                    if (WeightCount == 2)
                    {
                        output.Add((byte)VertexArray[i].BlendIndices.X);
                        output.Add((byte)VertexArray[i].BlendIndices.Y);
                    }
                    else if (WeightCount == 4)
                    {
                        output.Add((byte)VertexArray[i].BlendIndices.X);
                        output.Add((byte)VertexArray[i].BlendIndices.Y);
                        output.Add((byte)VertexArray[i].BlendIndices.Z);
                        output.Add((byte)VertexArray[i].BlendIndices.W);
                    }
                    else
                        throw new Exception("Unknown weight count");
                }

                return output.Distinct().ToList();
            }
            else
                throw new Exception("Unknown weight count");

        }

        public void UpdateAnimationIndecies(List<IndexRemapping> remapping)
        {
            for (int i = 0; i < VertexArray.Length; i++)
            {
                VertexArray[i].BlendIndices.X = GetMappedBlendIndex((byte)VertexArray[i].BlendIndices.X, remapping);
                VertexArray[i].BlendIndices.Y = GetMappedBlendIndex((byte)VertexArray[i].BlendIndices.Y, remapping);
                VertexArray[i].BlendIndices.Z = GetMappedBlendIndex((byte)VertexArray[i].BlendIndices.Z, remapping);
                VertexArray[i].BlendIndices.W = GetMappedBlendIndex((byte)VertexArray[i].BlendIndices.W, remapping);

                if (VertexFormat == UiVertexFormat.Weighted)
                {
                    VertexArray[i].BlendWeights.Z = 0;
                    VertexArray[i].BlendWeights.W = 0;
                }

                var totalBlendWeight = VertexArray[i].BlendWeights.X + VertexArray[i].BlendWeights.Y + VertexArray[i].BlendWeights.Z + VertexArray[i].BlendWeights.W;
                if ((1 - totalBlendWeight) >= float.Epsilon || (1 - totalBlendWeight) <= float.Epsilon)
                {
                    var factor = 1 / totalBlendWeight;
                    VertexArray[i].BlendWeights.X *= factor;
                    VertexArray[i].BlendWeights.Y *= factor;
                    VertexArray[i].BlendWeights.Z *= factor;
                    VertexArray[i].BlendWeights.W *= factor;
                }
                var totalBlendWeight2 = VertexArray[i].BlendWeights.X + VertexArray[i].BlendWeights.Y + VertexArray[i].BlendWeights.Z + VertexArray[i].BlendWeights.W;
            }

            RebuildVertexBuffer();
        }

        byte GetMappedBlendIndex(byte currentValue, List<IndexRemapping> remappingList)
        {
            var remappingItem = remappingList.FirstOrDefault(x => x.OriginalValue == currentValue);
            if (remappingItem != null)
                return (byte)remappingItem.NewValue;
            return currentValue;
        }

        public void Merge(List<MeshObject> others)//
        {
            var newVertexBufferSize = others.Sum(x => x.VertexCount()) + VertexCount();
            var newVertexArray = new VertexPositionNormalTextureCustom[newVertexBufferSize];

            // Copy current vertex buffer
            int currentVertexIndex = 0;
            for (; currentVertexIndex < VertexCount(); currentVertexIndex++)
                newVertexArray[currentVertexIndex] = VertexArray[currentVertexIndex];

            // Index buffers
            var newIndexBufferSize = others.Sum(x => x.GetIndexCount()) + GetIndexCount();
            var newIndexArray = new ushort[newIndexBufferSize];

            // Copy current index buffer
            int currentIndexIndex = 0;
            for (; currentIndexIndex < GetIndexCount(); currentIndexIndex++)
                newIndexArray[currentIndexIndex] = IndexArray[currentIndexIndex];

            // Copy others into main
            foreach (var geo in others)
            {
                ushort geoOffset = (ushort)(currentVertexIndex);
                int geoVertexIndex = 0;
                for (; geoVertexIndex < geo.VertexCount();)
                    newVertexArray[currentVertexIndex++] = geo.VertexArray[geoVertexIndex++];

                int geoIndexIndex = 0;
                for (; geoIndexIndex < geo.GetIndexCount();)
                    newIndexArray[currentIndexIndex++] = (ushort)(geo.IndexArray[geoIndexIndex++] + geoOffset); ;
            }


            VertexArray = newVertexArray;
            IndexArray = newIndexArray;

            RebuildIndexBuffer();
            RebuildVertexBuffer();
        }

        public void ChangeVertexType(UiVertexFormat newFormat, string newSkeletonName, bool updateMesh = true)
        {
            if (!(newFormat == UiVertexFormat.Weighted || newFormat == UiVertexFormat.Static || newFormat == UiVertexFormat.Cinematic))
                throw new Exception("Not able to change vertex format into this");

            if (updateMesh)
            {
                for (int i = 0; i < VertexArray.Length; i++)
                {
                    if (newFormat != UiVertexFormat.Static)
                    {
                        var vertInfo = new (float index, float weight)[4];
                        vertInfo[0] = (VertexArray[i].BlendIndices.X, VertexArray[i].BlendWeights.X);
                        vertInfo[1] = (VertexArray[i].BlendIndices.Y, VertexArray[i].BlendWeights.Y);
                        vertInfo[2] = (VertexArray[i].BlendIndices.Z, VertexArray[i].BlendWeights.Z);
                        vertInfo[3] = (VertexArray[i].BlendIndices.W, VertexArray[i].BlendWeights.W);

                        var sortedVertInfo = vertInfo.OrderByDescending(x => x.weight);

                        VertexArray[i].BlendIndices = new Vector4(sortedVertInfo.First().index, 0, 0, 0);
                        VertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }
                    else
                    {
                        VertexArray[i].BlendIndices = new Vector4(0, 0, 0, 0);
                        VertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }
                }

                RebuildVertexBuffer();
            }

            switch (newFormat)
            {
                case UiVertexFormat.Static:
                    WeightCount = 0;
                    ParentSkeletonName = "";
                    break;
                case UiVertexFormat.Weighted:
                    WeightCount = 2;
                    ParentSkeletonName = newSkeletonName;
                    break;
                case UiVertexFormat.Cinematic:
                    WeightCount = 4;
                    ParentSkeletonName = newSkeletonName;
                    break;
            }

            VertexFormat = newFormat;
        }

        public VertexPositionNormalTextureCustom GetVertexExtented(int index)//
        {
            return VertexArray[index];
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

        public int GetIndex(int i)//
        {
            return IndexArray[i];
        }


        public int GetIndexCount()//
        {
            return IndexArray.Length;
        }

        public List<ushort> GetIndexBuffer()//
        {
            return IndexArray.ToList();
        }

        public void SetIndexBuffer(List<ushort> buffer)
        {
            IndexArray = buffer.ToArray();
            RebuildIndexBuffer();
        }

        public void BuildBoundingBox()
        {
            var count = VertexCount();
            if (count == 0)
            {
                BoundingBox = new BoundingBox(-Vector3.One, Vector3.One);
                MeshCenter = Vector3.Zero;
                return;
            }

            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = GetVertexById(i);
            BoundingBox = BoundingBox.CreateFromPoints(points);

            // Update mesh center
            var corners = BoundingBox.GetCorners();
            MeshCenter = Vector3.Zero;
            for (int i = 0; i < corners.Length; i++)
                MeshCenter += corners[i];
            MeshCenter = MeshCenter / corners.Length;
        }

        public virtual void Dispose()
        {
            Context.Dispose();
        }

        public void RemoveFaces(List<int> facesToDelete)
        {
            var newIndexList = new ushort[IndexArray.Length - (facesToDelete.Count * 3)];
            var writeIndex = 0;
            for (int i = 0; i < IndexArray.Length;)
            {
                if (facesToDelete.Contains(i) == false)
                    newIndexList[writeIndex++] = IndexArray[i++];
                else
                    i += 3;
            }

            RemoveUnusedVertexes(newIndexList);
        }

        public void RemoveUnusedVertexes(ushort[] newIndexList)
        {
            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            var newVertexList = new List<VertexPositionNormalTextureCustom>();
            Dictionary<ushort, ushort> remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < VertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(VertexArray[i]);
                }
            }

            for (int i = 0; i < newIndexList.Length; i++)
                newIndexList[i] = remappingTable[newIndexList[i]];

            IndexArray = newIndexList;
            VertexArray = newVertexList.ToArray();

            RebuildIndexBuffer();
            RebuildVertexBuffer();
        }

        public MeshObject CloneSubMesh(ushort[] newIndexList)
        {
            var mesh = Clone(false) as MeshObject;

            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            var newVertexList = new List<VertexPositionNormalTextureCustom>();
            Dictionary<ushort, ushort> remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < VertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(VertexArray[i]);
                }
            }

            for (int i = 0; i < newIndexList.Length; i++)
                newIndexList[i] = remappingTable[newIndexList[i]];

            mesh.IndexArray = newIndexList;
            mesh.VertexArray = newVertexList.ToArray();

            mesh.RebuildIndexBuffer();
            mesh.RebuildVertexBuffer();

            return mesh;
        }

        public int VertexCount()//
        {
            return VertexArray.Length;
        }

        public void RebuildVertexBuffer()
        {
            Context.RebuildVertexBuffer(VertexArray, VertexPositionNormalTextureCustom.VertexDeclaration);
            BuildBoundingBox();
        }

        public void RebuildIndexBuffer()
        {
            Context.RebuildIndexBuffer(IndexArray);
        }
    }
}
