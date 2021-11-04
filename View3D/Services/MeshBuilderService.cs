using Filetypes.RigidModel;
using Filetypes.RigidModel.Transforms;
using Filetypes.RigidModel.Vertex;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Services
{
    public class MeshBuilderService
    {
        public static MeshObject BuildMeshFromRmvModel(RmvSubModel modelPart, string skeletonName, IGraphicsCardGeometry context)
        {
            var output = new MeshObject(context, skeletonName);
            output.Alpha = modelPart.GetAlphaMode();

            output.Pivot = new Vector3(modelPart.Header.Transform.Pivot.X, modelPart.Header.Transform.Pivot.Y, modelPart.Header.Transform.Pivot.Z);
            output.VertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            output.IndexArray = (ushort[])modelPart.Mesh.IndexList.Clone();
            output.ChangeVertexType(modelPart.Header.VertextType);

            for (int i = 0; i < modelPart.Mesh.VertexList.Length; i++)
            {
                var vertex = modelPart.Mesh.VertexList[i];
                output.VertexArray[i].Position = vertex.Postition.ToVector4(1);
                output.VertexArray[i].Normal = vertex.Normal.ToVector3();
                output.VertexArray[i].BiNormal = vertex.BiNormal.ToVector3();
                output.VertexArray[i].Tangent = vertex.Tangent.ToVector3();
                output.VertexArray[i].TextureCoordinate = vertex.Uv.ToVector2();

                if (output.VertexFormat == VertexFormat.Static)
                {
                    output.VertexArray[i].BlendIndices = Vector4.Zero;
                    output.VertexArray[i].BlendWeights = Vector4.Zero;
                }
                else if (output.VertexFormat == VertexFormat.Weighted)
                {
                    output.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
                    output.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                }
                else if (output.VertexFormat == VertexFormat.Cinematic)
                {

                    output.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
                    output.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                }
                else
                    throw new Exception("Unkown vertex format");
            }

            output.RebuildVertexBuffer();
            output.RebuildIndexBuffer();

            return output;
        }



        public static RmvSubModel CreateRmvSubModel(RmvSubModel baseModel, MeshObject geometry, RmvVersionEnum version)
        {
            var newSubModel = baseModel.Clone();
            newSubModel.SetAlphaMode(geometry.Alpha);
            newSubModel.Mesh = CreateRmvFileMesh(geometry, version);
            return newSubModel;
        }

        public static RmvMesh CreateRmvFileMesh(MeshObject geometry, RmvVersionEnum version)
        {
            RmvMesh mesh = new RmvMesh();
            mesh.IndexList = geometry.GetIndexBuffer().ToArray();

            // Ensure normalized
            for (int i = 0; i < geometry.VertexArray.Length; i++)
            {
                geometry.VertexArray[i].Normal = Vector3.Normalize(geometry.VertexArray[i].Normal);
                geometry.VertexArray[i].BiNormal = Vector3.Normalize(geometry.VertexArray[i].BiNormal);
                geometry.VertexArray[i].Tangent = Vector3.Normalize(geometry.VertexArray[i].Tangent);
            }

            switch (geometry.VertexFormat)
            {
                case VertexFormat.Static:
                    mesh.VertexList = new StaticVertex[geometry.VertexCount()];
                    break;

                case VertexFormat.Weighted:
                    mesh.VertexList = new WeightedVertex[geometry.VertexCount()];
                    break;

                case VertexFormat.Cinematic:
                    mesh.VertexList = new CinematicVertex[geometry.VertexCount()];
                    break;

                default:
                    throw new NotImplementedException();
            }

            
            for (int i = 0; i < mesh.VertexList.Length; i++)
            {
                var pos = new RmvVector4(geometry.VertexArray[i].Position.X, geometry.VertexArray[i].Position.Y, geometry.VertexArray[i].Position.Z, 1);
                var uv = new RmvVector2(geometry.VertexArray[i].TextureCoordinate.X, geometry.VertexArray[i].TextureCoordinate.Y);
                var normal = new RmvVector3(geometry.VertexArray[i].Normal.X, geometry.VertexArray[i].Normal.Y, geometry.VertexArray[i].Normal.Z);
                var biNormal = new RmvVector3(geometry.VertexArray[i].BiNormal.X, geometry.VertexArray[i].BiNormal.Y, geometry.VertexArray[i].BiNormal.Z);
                var tanget = new RmvVector3(geometry.VertexArray[i].Tangent.X, geometry.VertexArray[i].Tangent.Y, geometry.VertexArray[i].Tangent.Z);

                BaseVertex.ColourData? colourData = null;
                if (version == RmvVersionEnum.RMV2_V8)
                {
                    colourData = new BaseVertex.ColourData()
                    {
                        Colour = new byte[4] { 0, 0, 0, 255 }
                    };
                }

                switch (geometry.VertexFormat)
                {
                    case VertexFormat.Static:
                        mesh.VertexList[i] = new StaticVertex(pos, uv, normal, biNormal, tanget);
                        break;

                    case VertexFormat.Weighted:             
                        var boneBlendInfo2 =  new BaseVertex.BoneInformation[2]
                        {
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.X, geometry.VertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Y, geometry.VertexArray[i].BlendWeights.Y),
                        };

                        mesh.VertexList[i] = new WeightedVertex(pos, uv, normal, biNormal, tanget, boneBlendInfo2, colourData);
                        break;

                    case VertexFormat.Cinematic:
                        var boneBlendInfo4 = new BaseVertex.BoneInformation[4]
                        {
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.X, geometry.VertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Y, geometry.VertexArray[i].BlendWeights.Y),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Z, geometry.VertexArray[i].BlendWeights.Z),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.W, geometry.VertexArray[i].BlendWeights.W)
                        };

                        mesh.VertexList[i] = new CinematicVertex(pos, uv, normal, biNormal, tanget, boneBlendInfo4, colourData);
                        break;
                }

            }

            return mesh;
        }
    }
}
