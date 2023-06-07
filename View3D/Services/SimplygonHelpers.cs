using Simplygon;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering.Geometry;
using SharpDX.Direct3D9;
using Microsoft.Xna.Framework;

namespace View3D.Services
{
    /// <summary>
    /// Collections of methods to interact with simplygon, do the dirt work
    /// </summary>
    public class SimplygonHelpers
    {
        public static void InitSGGeometryDataObject(ISimplygon sg, out spGeometryData geometryDataSG, MeshObject originalMesh)
        {
            // -- create SG GeomtryData object and allocate space for mesh
            geometryDataSG = sg.CreateGeometryData();            
            geometryDataSG.SetTriangleCount(((uint)originalMesh.GetIndexCount()) / 3);
            geometryDataSG.SetVertexCount((uint)originalMesh.GetVertexList().Count);

            geometryDataSG.AddBoneWeights(4); 

            geometryDataSG.AddNormals();
            geometryDataSG.AddTangents(0);

            geometryDataSG.AddTexCoords(0); // add two texcoord channels, if format is "default" (static props)
            geometryDataSG.AddTexCoords(1); // TODO: AE should support 2 texture channel as some "default"/static models use it

        }
        public static void FillSGVertices(ref spGeometryData geometryDataSG, MeshObject originalMesh)
        {         
            for (int vertexIndex = 0; vertexIndex < originalMesh.VertexArray.Length; vertexIndex++)
            {
                ref var sourceVertex = ref originalMesh.VertexArray[vertexIndex];

                // Allocate pos (x,y,z)
                var positions = geometryDataSG.GetCoords();
                positions.SetTupleSize(3);
                positions.SetTuple(vertexIndex, new float[] { sourceVertex.Position3().X, sourceVertex.Position3().Y, sourceVertex.Position3().Z });

                // Allocate bone indices[4]
                var boneIndices = geometryDataSG.GetBoneIds();
                boneIndices.SetTupleSize(4);
                boneIndices.SetTuple(vertexIndex, new int[] { (int)sourceVertex.BlendIndices.X, (int)sourceVertex.BlendIndices.Y, (int)sourceVertex.BlendIndices.Z, (int)sourceVertex.BlendIndices.W });

                // Allocate bone weights[4]
                var boneWeights = geometryDataSG.GetBoneWeights();
                boneWeights.SetTupleSize(4);
                boneWeights.SetTuple(vertexIndex, new float[] { sourceVertex.BlendWeights.X, sourceVertex.BlendWeights.Y, sourceVertex.BlendWeights.Z, sourceVertex.BlendWeights.W });
            }
        }
        public static void FillSGTriangles(ref spGeometryData geometryDataSG, MeshObject originalMesh)
        {
            // -- construct triangles, and add values for polygon corners
            for (int cornerIndex = 0; cornerIndex < originalMesh.IndexArray.Length; cornerIndex++) 
            {                
                ushort vertexIndex = originalMesh.IndexArray[cornerIndex];

                // set vertex index for the triangle corner
                geometryDataSG.GetVertexIds().SetItem(cornerIndex, vertexIndex);

                ref var sourceVertex = ref originalMesh.VertexArray[vertexIndex];

                // -- set tex_coords channel 1
                var TextureCoords1 = geometryDataSG.GetTexCoords(0);
                TextureCoords1.SetTupleSize(2);
                TextureCoords1.SetTuple(cornerIndex, new float[] { sourceVertex.TextureCoordinate.X, sourceVertex.TextureCoordinate.Y });

                // -- set tex_coords channel 2 (empty in Ole's "commom format")
                // TODO: extend "common format" to having 2 UV channels, as many/some "default/static" models use that
                var textureCoords2 = geometryDataSG.GetTexCoords(1);
                textureCoords2.SetTupleSize(2);
                textureCoords2.SetTuple(cornerIndex, new float[] { 0, 0 });

                // -- Set normals
                var normals = geometryDataSG.GetNormals();
                normals.SetTupleSize(3);
                normals.SetTuple(cornerIndex, new float[] { sourceVertex.Normal.X, sourceVertex.Normal.Y, sourceVertex.Normal.Z });

                // -- Set tangents
                var tangents = geometryDataSG.GetTangents(0);
                tangents.SetTupleSize(3);
                tangents.SetTuple(cornerIndex, new float[] { sourceVertex.Tangent.X, sourceVertex.Tangent.Y, sourceVertex.Tangent.Z });

                // -- Set bitangents
                var bitangents = geometryDataSG.GetBitangents(0);
                bitangents.SetTupleSize(3);
                bitangents.SetTuple(cornerIndex, new float[] { sourceVertex.BiNormal.X, sourceVertex.BiNormal.Y, sourceVertex.BiNormal.Z });
            }
        }                
        public static void ReduceSGMesh(ISimplygon sg, spGeometryData pGeometryData, out spPackedGeometryData packedGeometryDataSG, float factor)
        {
            // -- create scene graph and add mesh
            var sgScene = sg.CreateScene();
            var mesh = sg.CreateSceneMesh();
            mesh.SetGeometry(pGeometryData);
            sgScene.GetRootNode().AddChild(mesh);
            
            // -- create SG mesh reduction processor, which only exists in the following scope, and thus is cleaned up at the end
            using (var sgReductionProcessor = sg.CreateReductionProcessor())
            {                
                sgReductionProcessor.SetScene(sgScene);

                Simplygon.spReductionSettings sgReductionSettings = sgReductionProcessor.GetReductionSettings();                

                // -- Set reduction stop condition and reduction ratio
                sgReductionSettings.SetReductionTargets(Simplygon.EStopCondition.All, true, false, false, false);
                sgReductionSettings.SetReductionTargetTriangleRatio(factor);

                // -- Set priorities for mesh reduction prevervation                
                sgReductionSettings.SetEdgeSetImportance(1.0f);
                sgReductionSettings.SetGeometryImportance(1.0f);
                sgReductionSettings.SetGroupImportance(1.0f);
                sgReductionSettings.SetMaterialImportance(1.0f);
                sgReductionSettings.SetShadingImportance(1.0f);
                sgReductionSettings.SetSkinningImportance(1.0f);
                sgReductionSettings.SetTextureImportance(1.0f);
                sgReductionSettings.SetVertexColorImportance(1.0f);

                // -- Do the processing
                sgReductionProcessor.RunProcessing();
            }            

            // -- convert to "packed vertex", where all "info" is in the vertex struct, for rendering/Rmv2
            packedGeometryDataSG = pGeometryData.NewPackedCopy();
            
        }
        public static void CopySGIndicesToMesh(spPackedGeometryData packedGeometryDataSG, ref MeshObject destMesh)
        {
            // -- Get the triangle indices from the SG object and store in MeshObject
            var indices = packedGeometryDataSG.GetVertexIds();
            for (int cornerIndex = 0; cornerIndex < packedGeometryDataSG.GetVertexIds().GetItemCount(); cornerIndex++)
            {
                destMesh.IndexArray[cornerIndex] = (ushort)indices.GetItem(cornerIndex);
            }
        }
        public static void CopySGVerticesToMesh(spPackedGeometryData packedGeomtryDataSG, ref MeshObject clone)
        {
            // -- Get the vertex data from the SG object and store in MeshObject
            for (int vertexIndex = 0; vertexIndex < clone.VertexArray.Length; vertexIndex++)
            {
                ref var detVertex = ref clone.VertexArray[vertexIndex];

                // pos (x,y,z)
                var positionsArray = packedGeomtryDataSG.GetCoords();
                var positionTuple = positionsArray.GetTuple(vertexIndex);
                detVertex.Position = new Vector4(positionTuple.GetItem(0), positionTuple.GetItem(1), positionTuple.GetItem(2), 1);
                positionTuple.Dispose();

                // -- Copy bone indices
                var boneIndicesArray = packedGeomtryDataSG.GetBoneIds();
                var boneIndexTuple = boneIndicesArray.GetTuple(vertexIndex);
                detVertex.BlendIndices = new Vector4(boneIndexTuple.GetItem(0), boneIndexTuple.GetItem(1), boneIndexTuple.GetItem(2), boneIndexTuple.GetItem(3));                
                boneIndexTuple.Dispose(); // INFO: I have to do this to avoid SG crashing on GC cleanup. Could avoid "tuple" and calc indexes "manually", but would look messier

                // -- Copy bone weights
                var boneWeightArray = packedGeomtryDataSG.GetBoneWeights();
                var boneWeightTuple = boneWeightArray.GetTuple(vertexIndex);
                detVertex.BlendWeights = new Vector4(boneWeightArray.GetItem(0), boneWeightArray.GetItem(1), boneWeightArray.GetItem(2), boneWeightArray.GetItem(3));
                boneWeightTuple.Dispose();

                // -- Copy UVs
                var textureCoords1 = packedGeomtryDataSG.GetTexCoords(0);
                var TextureCoord1Tuple = textureCoords1.GetTuple(vertexIndex);
                detVertex.TextureCoordinate = new Vector2(TextureCoord1Tuple.GetItem(0), TextureCoord1Tuple.GetItem(1));
                TextureCoord1Tuple.Dispose();

                // set normals
                var normalsArray = packedGeomtryDataSG.GetNormals();
                var normalTuple = normalsArray.GetTuple(vertexIndex);
                detVertex.Normal = new Vector3(normalTuple.GetItem(0), normalTuple.GetItem(1), normalTuple.GetItem(2));
                normalTuple.Dispose();

                // set tangents
                var tangentsArray = packedGeomtryDataSG.GetTangents(0);
                var tangentTuple = tangentsArray.GetTuple(vertexIndex);
                detVertex.Tangent = new Vector3(tangentTuple.GetItem(0), tangentTuple.GetItem(1), tangentTuple.GetItem(2));
                tangentTuple.Dispose();

                // set bitangents
                var bitangentsArray = packedGeomtryDataSG.GetBitangents(0);
                var bitangentsTuple = bitangentsArray.GetTuple(vertexIndex);
                detVertex.BiNormal = new Vector3(bitangentsTuple.GetItem(0), bitangentsTuple.GetItem(1), bitangentsTuple.GetItem(2));
                bitangentsTuple.Dispose();
            }
        }
    }    
}
