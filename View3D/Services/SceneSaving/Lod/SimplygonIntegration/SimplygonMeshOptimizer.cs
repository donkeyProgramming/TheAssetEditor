using Microsoft.Xna.Framework;
using Simplygon;
using View3D.Rendering;
using View3D.Rendering.Geometry;


namespace View3D.Services.SceneSaving.Lod.SimplygonIntegration
{
    public class SimplygonMeshOptimizer
    {
        public static MeshObject GetReducedMeshCopy(ISimplygon sg, MeshObject originalMesh, float factor)
        {
            var geometryData = InitSimplygonGeometry(sg, originalMesh);
            FillGeometryVertices(geometryData, originalMesh);
            FillGeometryTriangles(geometryData, originalMesh);
            var reducedPackedGeometryData = GetReducedPackedGeometry(sg, geometryData, factor);

            return GetMeshFromPackedGeometry(originalMesh, reducedPackedGeometryData);
        }

        private static spGeometryData InitSimplygonGeometry(ISimplygon sg, MeshObject originalMesh)
        {
            // -- create SG GeomtryData object and allocate space for mesh
            var geometryData = sg.CreateGeometryData();
            geometryData.SetTriangleCount((uint)originalMesh.GetIndexCount() / 3);
            geometryData.SetVertexCount((uint)originalMesh.GetVertexList().Count);

            geometryData.AddBoneWeights(4);

            geometryData.AddNormals();
            geometryData.AddTangents(0);

            geometryData.AddTexCoords(0); // add two texcoord channels, if format is "default" (static props)
            geometryData.AddTexCoords(1); // TODO: AE should support 2 texture channel as some "default"/static models use it

            return geometryData;
        }
        private static void FillGeometryVertices(spGeometryData geometryDataSG, MeshObject originalMesh)
        {
            for (var vertexIndex = 0; vertexIndex < originalMesh.VertexArray.Length; vertexIndex++)
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
        private static void FillGeometryTriangles(spGeometryData geometryDataSG, MeshObject originalMesh)
        {
            // -- construct triangles, and add values for polygon corners
            for (var cornerIndex = 0; cornerIndex < originalMesh.IndexArray.Length; cornerIndex++)
            {
                var vertexIndex = originalMesh.IndexArray[cornerIndex];

                // set vertex index for the triangle corner
                geometryDataSG.GetVertexIds().SetItem(cornerIndex, vertexIndex);

                ref var sourceVertex = ref originalMesh.VertexArray[vertexIndex];

                // -- set tex_coords channel 1
                var textureCoords1 = geometryDataSG.GetTexCoords(0);
                textureCoords1.SetTupleSize(2);
                textureCoords1.SetTuple(cornerIndex, new float[] { sourceVertex.TextureCoordinate.X, sourceVertex.TextureCoordinate.Y });

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
        private static spPackedGeometryData GetReducedPackedGeometry(ISimplygon sg, spGeometryData geometryDataSG, float factor)
        {
            // -- create scene graph and add mesh
            var sgScene = sg.CreateScene();
            var mesh = sg.CreateSceneMesh();
            mesh.SetGeometry(geometryDataSG);
            sgScene.GetRootNode().AddChild(mesh);

            // -- create SG mesh reduction processor, which only exists in the following scope, and thus is cleaned up at the end
            using (var reductionProcessor = sg.CreateReductionProcessor())
            {
                reductionProcessor.SetScene(sgScene);

                var reductionSettings = reductionProcessor.GetReductionSettings();

                // -- Set reduction stop condition and reduction ratio
                reductionSettings.SetReductionTargets(EStopCondition.All, true, false, false, false);
                reductionSettings.SetReductionTargetTriangleRatio(factor);

                // -- Set priorities for mesh reduction prevervation                
                reductionSettings.SetEdgeSetImportance(1.0f);
                reductionSettings.SetGeometryImportance(1.0f);
                reductionSettings.SetGroupImportance(1.0f);
                reductionSettings.SetMaterialImportance(1.0f);
                reductionSettings.SetShadingImportance(1.0f);
                reductionSettings.SetSkinningImportance(1.0f);
                reductionSettings.SetTextureImportance(1.0f);
                reductionSettings.SetVertexColorImportance(1.0f);

                // -- Do the processing
                reductionProcessor.RunProcessing();
            }

            // -- convert to "packed vertex", where all "info" is in the vertex struct, for rendering/Rmv2
            return geometryDataSG.NewPackedCopy();

        }
        private static ushort[] GetIndiciesFromPackedGeometry(spPackedGeometryData packedGeometryData)
        {
            var sourceIndices = packedGeometryData.GetVertexIds();
            var destIndices = new ushort[sourceIndices.GetItemCount()];

            for (var cornerIndex = 0; cornerIndex < sourceIndices.GetItemCount(); cornerIndex++)
            {
                destIndices[cornerIndex] = (ushort)sourceIndices.GetItem(cornerIndex);
            }

            return destIndices;
        }
        private static VertexPositionNormalTextureCustom[] GetVerticesFromPackedGeometry(spPackedGeometryData packedGeomtryDataSG)
        {
            var sourceVertices = packedGeomtryDataSG.GetVertices();
            var destVertices = new VertexPositionNormalTextureCustom[packedGeomtryDataSG.GetVertexCount()];

            for (var vertexIndex = 0; vertexIndex < packedGeomtryDataSG.GetVertexCount(); vertexIndex++)
            {
                ref var detVertex = ref destVertices[vertexIndex];

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

            return destVertices;
        }
        private static MeshObject GetMeshFromPackedGeometry(MeshObject originalMesh, spPackedGeometryData newPackedDataSG)
        {
            // -- Make new MeshObject to fit reduces mesh
            var reducedMesh = originalMesh.Clone(false);
            reducedMesh.VertexArray = GetVerticesFromPackedGeometry(newPackedDataSG);
            reducedMesh.IndexArray = GetIndiciesFromPackedGeometry(newPackedDataSG);

            // -- Update VRAM buffers
            reducedMesh.RebuildIndexBuffer();
            reducedMesh.RebuildVertexBuffer();
            return reducedMesh;
        }
    }
}
