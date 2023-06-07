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
    /// Collections of methods to interact with simplygo
    /// </summary>
    public class SimplygonHelpers
    {
        public static void InitSGGeometryDataObject(ISimplygon sg, out spGeometryData pGeometryData, MeshObject originalMesh)
        {
            // -- create SG GeomtryData object and allocate space for mesh
            pGeometryData = sg.CreateGeometryData();            
            pGeometryData.SetTriangleCount(((uint)originalMesh.GetIndexCount()) / 3);
            pGeometryData.SetVertexCount((uint)originalMesh.GetVertexList().Count);

            pGeometryData.AddBoneWeights(4); 

            pGeometryData.AddNormals();
            pGeometryData.AddTangents(0);

            pGeometryData.AddTexCoords(0); // add two texcoord channels, if format is "default" (static props)
            pGeometryData.AddTexCoords(1); // TODO: AE should support 2 texture channel as some "default"/static models use it

        }
        public static void FillSGVertices(ref spGeometryData geometryDataSG, MeshObject originalMesh)
        {         
            for (int vertexIndex = 0; vertexIndex < originalMesh.VertexArray.Length; vertexIndex++)
            {
                ref var srcVertex = ref originalMesh.VertexArray[vertexIndex];

                // pos (x,y,z)
                var coords = geometryDataSG.GetCoords();
                coords.SetTupleSize(3);
                coords.SetTuple(vertexIndex, new float[] { srcVertex.Position3().X, srcVertex.Position3().Y, srcVertex.Position3().Z });

                // bone indices[4]
                var bone_ids = geometryDataSG.GetBoneIds();
                bone_ids.SetTupleSize(4);
                bone_ids.SetTuple(vertexIndex, new int[] { (int)srcVertex.BlendIndices.X, (int)srcVertex.BlendIndices.Y, (int)srcVertex.BlendIndices.Z, (int)srcVertex.BlendIndices.W });

                // bone weights[4]
                var bone_weights = geometryDataSG.GetBoneWeights();
                bone_weights.SetTupleSize(4);
                bone_weights.SetTuple(vertexIndex, new float[] { srcVertex.BlendWeights.X, srcVertex.BlendWeights.Y, srcVertex.BlendWeights.Z, srcVertex.BlendWeights.W });
            }
        }
        public static void FillSGTriangles(ref spGeometryData geometryDataSG, MeshObject originalMesh)
        {
            // -- construct triangles, and add values for polygon corners
            // -- construct triangles, and add values for polygon corners
            for (int cornerIndex = 0; cornerIndex < originalMesh.IndexArray.Length; cornerIndex++)
            {
                //auto & index = oMeshData.oUnpackedMesh.vecIndices[index];
                ushort vertexIndex = originalMesh.IndexArray[cornerIndex];

                // set vertex index for the triangle corner
                geometryDataSG.GetVertexIds().SetItem(cornerIndex, vertexIndex);

                ref var vertex = ref originalMesh.VertexArray[vertexIndex];

                // -- set tex_coords channel 1
                var tex_coords = geometryDataSG.GetTexCoords(0);
                tex_coords.SetTupleSize(2);
                tex_coords.SetTuple(cornerIndex, new float[] { vertex.TextureCoordinate.X, vertex.TextureCoordinate.Y });

                // -- set tex_coords channel 2 (empty in Ole's "commom format")
                // TODO: extend "common format" to having 2 UV channels, as many/some "default/static" models use that
                var tex_coords2 = geometryDataSG.GetTexCoords(1);
                tex_coords2.SetTupleSize(2);
                tex_coords2.SetTuple(cornerIndex, new float[] { 0, 0 });

                // -- Set normals
                var normals = geometryDataSG.GetNormals();
                normals.SetTupleSize(3);
                normals.SetTuple(cornerIndex, new float[] { vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z });

                // -- Set tangents
                var tangents = geometryDataSG.GetTangents(0);
                tangents.SetTupleSize(3);
                tangents.SetTuple(cornerIndex, new float[] { vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z });

                // -- Set bitangents
                var bitangents = geometryDataSG.GetBitangents(0);
                bitangents.SetTupleSize(3);
                bitangents.SetTuple(cornerIndex, new float[] { vertex.BiNormal.X, vertex.BiNormal.Y, vertex.BiNormal.Z });
            }
        }                
        public static void ReduceSGMesh(ISimplygon sg, spGeometryData pGeometryData, out spPackedGeometryData packedGeometryDataSG, float factor)
        {
            //******************************************************************************************
            //	Reduce the mesh
            //******************************************************************************************
            var triangle_count_before = pGeometryData.GetTriangleCount();

            // -- create scene and add mesh
            var sgScene = sg.CreateScene();
            var mesh = sg.CreateSceneMesh();
            mesh.SetGeometry(pGeometryData);
            sgScene.GetRootNode().AddChild(mesh);

            // -- create SG mesh reduction processor, which only exists in the following scope, and thus is cleaned up at the end
            using (var sgReductionProcessor = sg.CreateReductionProcessor())
            {
                //var  = sg.CreateReductionProcessor();
                sgReductionProcessor.SetScene(sgScene);

                Simplygon.spReductionSettings sgReductionSettings = sgReductionProcessor.GetReductionSettings();
                //Simplygon::spRepairSettings sgRepairSettings = g_sgReductionProcessor->GetRepairSettings();

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
            var triangle_count_after_unpacked = pGeometryData.GetTriangleCount();

            packedGeometryDataSG = pGeometryData.NewPackedCopy();

            /******************************************************************************************
                Copy the reduced mesh back into buffer
            *******************************************************************************************/
            var vertexCountAfter = packedGeometryDataSG.GetVertexCount();


            //auto before = triangle_count_before;
            var triangle_count_after = packedGeometryDataSG.GetTriangleCount();


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
                boneIndexTuple.Dispose(); // INFO: I have to do this to avoid SG crashing on cleanup.

                // -- Copy bone weights
                var boneWeightArray = packedGeomtryDataSG.GetBoneWeights();
                var boneWeightTuple = boneWeightArray.GetTuple(vertexIndex);
                detVertex.BlendWeights = new Vector4(boneWeightArray.GetItem(0), boneWeightArray.GetItem(1), boneWeightArray.GetItem(2), boneWeightArray.GetItem(3));
                boneWeightTuple.Dispose();

                // -- Copy UVs
                var tex_coords = packedGeomtryDataSG.GetTexCoords(0);
                var tex_coords_data = tex_coords.GetTuple(vertexIndex);
                detVertex.TextureCoordinate = new Vector2(tex_coords_data.GetItem(0), tex_coords_data.GetItem(1));
                tex_coords_data.Dispose();

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
