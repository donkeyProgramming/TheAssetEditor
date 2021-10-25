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
        public static Rmv2Geometry BuildMeshFromRmvModel(RmvSubModel modelPart, IGeometryGraphicsContext context)
        {
            var output = new Rmv2Geometry(context);

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

        public static RmvMesh CreateRmvFileMesh(Rmv2Geometry geometry)
        {
            RmvMesh mesh = new RmvMesh();
            mesh.IndexList = geometry.GetIndexBuffer().ToArray();

            // Ensure normalized
            for (int i = 0; i < mesh.VertexList.Length; i++)
            {
                geometry.VertexArray[i].Normal = Vector3.Normalize(geometry.VertexArray[i].Normal);
                geometry.VertexArray[i].BiNormal = Vector3.Normalize(geometry.VertexArray[i].BiNormal);
                geometry.VertexArray[i].Tangent = Vector3.Normalize(geometry.VertexArray[i].Tangent);
            }

            if (geometry.VertexFormat == VertexFormat.Static)
            {
                mesh.VertexList = new DefaultVertex[geometry.VertexCount()];

                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    mesh.VertexList[i] = new DefaultVertex(
                        new RmvVector4(geometry.VertexArray[i].Position.X, geometry.VertexArray[i].Position.Y, geometry.VertexArray[i].Position.Z, 1),
                        new RmvVector2(geometry.VertexArray[i].TextureCoordinate.X, geometry.VertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(geometry.VertexArray[i].Normal.X, geometry.VertexArray[i].Normal.Y, geometry.VertexArray[i].Normal.Z),
                        new RmvVector3(geometry.VertexArray[i].BiNormal.X, geometry.VertexArray[i].BiNormal.Y, geometry.VertexArray[i].BiNormal.Z),
                        new RmvVector3(geometry.VertexArray[i].Tangent.X, geometry.VertexArray[i].Tangent.Y, geometry.VertexArray[i].Tangent.Z)
                      );
                }
            }
            else if (geometry.VertexFormat == VertexFormat.Weighted)
            {
                mesh.VertexList = new WeightedVertex[geometry.VertexCount()];
                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    mesh.VertexList[i] = new WeightedVertex(
                        new RmvVector4(geometry.VertexArray[i].Position.X, geometry.VertexArray[i].Position.Y, geometry.VertexArray[i].Position.Z, 1),
                        new RmvVector2(geometry.VertexArray[i].TextureCoordinate.X, geometry.VertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(geometry.VertexArray[i].Normal.X, geometry.VertexArray[i].Normal.Y, geometry.VertexArray[i].Normal.Z),
                        new RmvVector3(geometry.VertexArray[i].BiNormal.X, geometry.VertexArray[i].BiNormal.Y, geometry.VertexArray[i].BiNormal.Z),
                        new RmvVector3(geometry.VertexArray[i].Tangent.X, geometry.VertexArray[i].Tangent.Y, geometry.VertexArray[i].Tangent.Z),
                        new BaseVertex.BoneInformation[2]
                        {
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.X, geometry.VertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Y, geometry.VertexArray[i].BlendWeights.Y),
                        });
                }
            }
            else if (geometry.VertexFormat == VertexFormat.Cinematic)
            {
                mesh.VertexList = new CinematicVertex[geometry.VertexCount()];
                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    mesh.VertexList[i] = new CinematicVertex(
                        new RmvVector4(geometry.VertexArray[i].Position.X, geometry.VertexArray[i].Position.Y, geometry.VertexArray[i].Position.Z, 1),
                        new RmvVector2(geometry.VertexArray[i].TextureCoordinate.X, geometry.VertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(geometry.VertexArray[i].Normal.X, geometry.VertexArray[i].Normal.Y, geometry.VertexArray[i].Normal.Z),
                        new RmvVector3(geometry.VertexArray[i].BiNormal.X, geometry.VertexArray[i].BiNormal.Y, geometry.VertexArray[i].BiNormal.Z),
                        new RmvVector3(geometry.VertexArray[i].Tangent.X, geometry.VertexArray[i].Tangent.Y, geometry.VertexArray[i].Tangent.Z),

                        new BaseVertex.BoneInformation[4]
                        {
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.X, geometry.VertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Y, geometry.VertexArray[i].BlendWeights.Y),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.Z, geometry.VertexArray[i].BlendWeights.Z),
                            new BaseVertex.BoneInformation( (byte)geometry.VertexArray[i].BlendIndices.W, geometry.VertexArray[i].BlendWeights.W)
                        });
                }
            }
            else
            {
                throw new Exception("Unknown vertex format");
            }

            return mesh;
        }
    }
}
