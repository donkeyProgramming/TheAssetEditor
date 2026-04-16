using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Editors.BoneMapping;

namespace GameWorld.Core.Rendering.Geometry
{
    public class MeshObject : IDisposable
    {
        IGraphicsCardGeometry Context;

        public VertexPositionNormalTextureCustom[] VertexArray; // Vector3 for pos at some point
        public ushort[] IndexArray;

        public BoundingBox BoundingBox { get; private set; }
        public Vector3 MeshCenter { get; private set; }

        public int WeightCount { get => GetWeightCount(); } // reduce the use of this
        public UiVertexFormat VertexFormat { get; private set; } = UiVertexFormat.Unknown;
        public string SkeletonName { get; private set; }  // SkeletonName


        public IGraphicsCardGeometry GetGeometryContext() => Context;



        public int GetWeightCount() => VertexFormat switch
        {
            UiVertexFormat.Cinematic => 4,
            UiVertexFormat.Weighted => 2,
            UiVertexFormat.Static => 0,

            _ => throw new Exception("Unknown vertex format for mesh")
        };


        public MeshObject(IGraphicsCardGeometry context, string skeletonName)
        {
            SkeletonName = skeletonName;
            Context = context;
        }

        public MeshObject Clone(bool includeMesh = true)
        {
            var mesh = new MeshObject(Context, SkeletonName)
            {
                Context = Context.Clone(),
                BoundingBox = BoundingBox,
                MeshCenter = MeshCenter,
                SkeletonName = SkeletonName,
                VertexFormat = VertexFormat
            };

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
            var output = new List<Vector3>(vertCount);
            for (var i = 0; i < vertCount; i++)
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
                for (var i = 0; i < VertexArray.Count(); i++)
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
            for (var i = 0; i < VertexArray.Length; i++)
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
                if (1 - totalBlendWeight >= float.Epsilon || 1 - totalBlendWeight <= float.Epsilon)
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
            var currentVertexIndex = 0;
            for (; currentVertexIndex < VertexCount(); currentVertexIndex++)
                newVertexArray[currentVertexIndex] = VertexArray[currentVertexIndex];

            // Index buffers
            var newIndexBufferSize = others.Sum(x => x.GetIndexCount()) + GetIndexCount();
            var newIndexArray = new ushort[newIndexBufferSize];

            // Copy current index buffer
            var currentIndexIndex = 0;
            for (; currentIndexIndex < GetIndexCount(); currentIndexIndex++)
                newIndexArray[currentIndexIndex] = IndexArray[currentIndexIndex];

            // Copy others into main
            foreach (var geo in others)
            {
                var geoOffset = (ushort)currentVertexIndex;
                var geoVertexIndex = 0;
                for (; geoVertexIndex < geo.VertexCount();)
                    newVertexArray[currentVertexIndex++] = geo.VertexArray[geoVertexIndex++];

                var geoIndexIndex = 0;
                for (; geoIndexIndex < geo.GetIndexCount();)
                    newIndexArray[currentIndexIndex++] = (ushort)(geo.IndexArray[geoIndexIndex++] + geoOffset); ;
            }

            VertexArray = newVertexArray;
            IndexArray = newIndexArray;

            RebuildIndexBuffer();
            RebuildVertexBuffer();
        }


        public void UpdateSkeletonName(string newSkeletonName)
        {
            SkeletonName = newSkeletonName;
        }

        public void ChangeVertexType(UiVertexFormat newFormat, bool updateMesh = true)
        {
            if (!(newFormat == UiVertexFormat.Weighted || newFormat == UiVertexFormat.Static || newFormat == UiVertexFormat.Cinematic))
                throw new Exception($"Not able to change vertex format into {newFormat}");

            if (VertexFormat == newFormat)
                return;

            VertexFormat = newFormat;
            if (VertexFormat == UiVertexFormat.Static)
                UpdateSkeletonName(string.Empty);

            if (updateMesh)
            {
                for (var i = 0; i < VertexArray.Length; i++)
                {
                    if (VertexFormat != UiVertexFormat.Static)
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
        }

        public VertexPositionNormalTextureCustom GetVertexExtented(int index)
        {
            return VertexArray[index];
        }

        public int GetIndex(int i)//
        {
            return IndexArray[i];
        }

        public int GetIndexCount()//
        {
            return IndexArray.Length;
        }

        public List<ushort> GetIndexBuffer()
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
            for (var i = 0; i < count; i++)
                points[i] = GetVertexById(i);
            BoundingBox = BoundingBox.CreateFromPoints(points);

            // Update mesh center
            var corners = BoundingBox.GetCorners();
            MeshCenter = Vector3.Zero;
            for (var i = 0; i < corners.Length; i++)
                MeshCenter += corners[i];
            MeshCenter = MeshCenter / corners.Length;
        }

        public void Dispose()
        {
            Context.Dispose();
        }

        public void RemoveFaces(List<int> facesToDelete)
        {
            var newIndexList = new ushort[IndexArray.Length - facesToDelete.Count * 3];
            var writeIndex = 0;
            for (var i = 0; i < IndexArray.Length;)
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
            var remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < VertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(VertexArray[i]);
                }
            }

            for (var i = 0; i < newIndexList.Length; i++)
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
            var remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < VertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(VertexArray[i]);
                }
            }

            for (var i = 0; i < newIndexList.Length; i++)
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
