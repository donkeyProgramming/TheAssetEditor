#include "MeshProcessorService.h"
#include "..\libs\MS_SimpleMath\SimpleMath.h"

void wrapdll::MeshProcessorService::DoFinalProcessing()
{    
    ComputeFaceTangentsUnindexed();
    DoMeshIndexingWithTangentSmoothing(); 
}

void wrapdll::MeshProcessorService::DoMeshIndexingWithTangentSmoothing()
{
    std::vector<PackedCommonVertex> outVertices;
    std::vector<uint32_t> outIndices;
    std::vector<uint32_t> outMapUsedVertices; // index = old index, value = new value            
    std::vector<uint32_t> outMapOldToNew; // index = old index, value = new value            

    auto timeLogger = TimeLogAction::PrintStart("Indexing...");

    // TODO: Decide Which version is ACTUALLY faster??
    // TODO: Bade Name! Sacrifice speed: Split this up in several methods(loops)?
    DoMeshIndexingWithTangenSmoothing_OutPutRemap_Slow(
        m_poDestMesh->vertices,
        outVertices,
        outIndices,
        outMapUsedVertices,
        outMapOldToNew
    );

    timeLogger.PrintDone();

    std::vector<VertexWeight> outVertexWeights;
    RemapVertexWeights(
        m_poDestMesh->vertexWeights,
        outVertexWeights,
        outMapUsedVertices,
        outMapOldToNew,
        static_cast<uint32_t>(outVertices.size()));

    m_poDestMesh->vertices = outVertices;
    m_poDestMesh->indices = outIndices;
    m_poDestMesh->vertexWeights = outVertexWeights;

    // TODO: debuging: decide if this check needed, to say in forever? 
    // -- Is there at least 1 weight for every vertex
    for (size_t vertexIndex = 0; vertexIndex < m_poDestMesh->vertices.size(); vertexIndex++)
    {
        bool bThereIsAWeight = false;
        for (auto& vertexWeight : outVertexWeights) // check all weighs, check is there is a weight for the current vertex
        {
            if (vertexWeight.vertexIndex == vertexIndex)
            {
                bThereIsAWeight = true;
            }
        }

        if (!bThereIsAWeight)
        {
            // TODO: check if the mesh is mean to have skin, then this is an error.
            //auto DEBUG_BREAK = 1;
            //LogActionError("Invalid Vertex Weights for mesh: " + m_destMesh.meshName + "a vertex has no weight");
        }
    }
}

void wrapdll::MeshProcessorService::ComputeTangentBasisForUnindexedMesh(const std::vector<sm::Vector3>& vertices, const std::vector<sm::Vector2>& uvs, std::vector<sm::Vector3>& tangents, std::vector<sm::Vector3>& bitangents)
{
    for (unsigned int i = 0; i < vertices.size(); i += 3) {
        // Shortcuts for vertices
        const sm::Vector3& v0 = vertices[i + 0];
        const sm::Vector3& v1 = vertices[i + 1];
        const sm::Vector3& v2 = vertices[i + 2];

        // Shortcuts for UVs
        const sm::Vector2& uv0 = uvs[i + 0];
        const sm::Vector2& uv1 = uvs[i + 1];
        const sm::Vector2& uv2 = uvs[i + 2];

        // Edges of the triangle : postion delta
        sm::Vector3 deltaPos1 = v1 - v0;
        sm::Vector3 deltaPos2 = v2 - v0;

        // UV delta
        sm::Vector2 deltaUV1 = uv1 - uv0;
        sm::Vector2 deltaUV2 = uv2 - uv0;

        float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
        sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
        sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

        // tangent/bitangent will be averages during indexing
        tangents.push_back(tangent);
        tangents.push_back(tangent);
        tangents.push_back(tangent);

        bitangents.push_back(bitangent);
        bitangents.push_back(bitangent);
        bitangents.push_back(bitangent);
    }

    return;
}

void wrapdll::MeshProcessorService::ComputeFaceTangentsUnindexed()
{
    for (unsigned int vertexInxdex = 0; vertexInxdex < m_poDestMesh->vertices.size(); vertexInxdex += 3)
    {
        const sm::Vector3 v0 = sm::Vector3::FromFloat4(m_poDestMesh->vertices[vertexInxdex + 0].position);
        const sm::Vector3 v1 = sm::Vector3::FromFloat4(m_poDestMesh->vertices[vertexInxdex + 1].position);
        const sm::Vector3 v2 = sm::Vector3::FromFloat4(m_poDestMesh->vertices[vertexInxdex + 2].position);

        // Shortcuts for UVs
        const sm::Vector2 uv0 = m_poDestMesh->vertices[vertexInxdex + 0].uv;
        const sm::Vector2 uv1 = m_poDestMesh->vertices[vertexInxdex + 1].uv;
        const sm::Vector2 uv2 = m_poDestMesh->vertices[vertexInxdex + 2].uv;

        // Edges of the triangle : postion delta
        sm::Vector3 deltaPos1 = v1 - v0;
        sm::Vector3 deltaPos2 = v2 - v0;

        // UV delta
        sm::Vector2 deltaUV1 = uv1 - uv0;
        sm::Vector2 deltaUV2 = uv2 - uv0;

        // calculate tangents
        float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
        sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
        sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

        // store same tangents for all corners of the face (="face tangents")
        m_poDestMesh->vertices[vertexInxdex + 0].tangent = tangent;
        m_poDestMesh->vertices[vertexInxdex + 1].tangent = tangent;
        m_poDestMesh->vertices[vertexInxdex + 2].tangent = tangent;

        m_poDestMesh->vertices[vertexInxdex + 0].bitangent = bitangent;
        m_poDestMesh->vertices[vertexInxdex + 1].bitangent = bitangent;
        m_poDestMesh->vertices[vertexInxdex + 2].bitangent = bitangent;
    }
}


/// <summary>
/// Makes an unindex mesh into an indexed one, by discarding indentical/very similar vertices.
/// Makes an index buffer for the new index mesh
/// Makes a "vertex remap", 
/// newIndex =  remap[oldIndex], -1 for discarded vertices
/// uses for vertex influence remapping,
/// (could be use later, to split the DoIndexing..() method up into discrete methods)
/// </summary>
/// <param name="inVertices">Unindexed vertex input buffer</param>
/// <param name="outVertices">Indexed vertex output buffer </param>
/// <param name="outIndices">Index buffer</param>
/// <param name="outVertexRemap">vertex remap</param>

inline void wrapdll::MeshProcessorService::DoMeshIndexingWithTangenSmoothing_Slow(const std::vector<PackedCommonVertex>& inVertices, std::vector<PackedCommonVertex>& outVertices, std::vector<uint32_t>& outIndices, std::vector<int>& outVertexRemap)
{
    outVertexRemap.clear(); // can never be too sure?:)        

    // For each input vertex
    for (unsigned int inVertexIndex = 0; inVertexIndex < inVertices.size(); inVertexIndex++) {

        // Try to find a similar vertex in out_XXXX
        uint32_t indexToMatchingVertex;

        bool matchingVertexFound = GetSimilarPackedVertexIndex_Slow(convert::ConvertToVec3(inVertices[inVertexIndex].position), inVertices[inVertexIndex].uv, inVertices[inVertexIndex].normal, outVertices, indexToMatchingVertex);

        if (matchingVertexFound) // A similar vertex is already in the new OUTPUT Vertex buffer, use that!
        {
            outIndices.push_back(indexToMatchingVertex); // refer to existing vertex+vertexweight

            // Average the tangents and the bitangents
            outVertices[indexToMatchingVertex].tangent = sm::Vector3(outVertices[indexToMatchingVertex].tangent) + sm::Vector3(inVertices[inVertexIndex].tangent);
            outVertices[indexToMatchingVertex].bitangent = sm::Vector3(outVertices[indexToMatchingVertex].bitangent) + sm::Vector3(inVertices[inVertexIndex].bitangent);

            outVertexRemap.push_back(VERTEX_DISCARDED); // this a duplicate                    
        }
        else // No matching vertex found, add a vertex from the INPUT vertex buffer
        {
            outVertices.push_back(inVertices[inVertexIndex]);

            uint32_t newVertexIndex = (uint32_t)outVertices.size() - 1;
            outIndices.push_back(newVertexIndex);

            outVertexRemap.push_back(newVertexIndex);
        }
    }
}

inline void wrapdll::MeshProcessorService::DoMeshIndexingWithTangenSmoothing_OutPutRemap_Slow(const std::vector<PackedCommonVertex>& inVertices, std::vector<PackedCommonVertex>& outVertices, std::vector<uint32_t>& outIndices, std::vector<uint32_t>& mapUsedOldVertices, std::vector<uint32_t>& mapOldToNew)
{
    mapUsedOldVertices.clear(); // can never be too sure?:)                    

    std::map<PackedVertex, uint32_t> VertexToOutIndex;
    mapOldToNew.resize(inVertices.size());
    // For each input vertex
    for (unsigned int iVertex = 0; iVertex < inVertices.size(); iVertex++)
    {
        uint32_t indexToMatchingVertex;

        bool matchingVertexFound = GetSimilarPackedVertexIndex_Slow(convert::ConvertToVec3(inVertices[iVertex].position), inVertices[iVertex].uv, inVertices[iVertex].normal, outVertices, indexToMatchingVertex);

        if (matchingVertexFound) // A similar vertex is already in the new OUTPUT Vertex buffer, use that!
        {
            outIndices.push_back(indexToMatchingVertex); // refer to existing vertex+vertexweight

            // Average the tangents and the bitangents
            outVertices[indexToMatchingVertex].tangent = sm::Vector3(outVertices[indexToMatchingVertex].tangent) + sm::Vector3(inVertices[iVertex].tangent);
            outVertices[indexToMatchingVertex].bitangent = sm::Vector3(outVertices[indexToMatchingVertex].bitangent) + sm::Vector3(inVertices[iVertex].bitangent);

            mapOldToNew[iVertex] = indexToMatchingVertex;
        }
        else // No matching vertex found, add a vertex from the INPUT vertex buffer
        {
            outVertices.push_back(inVertices[iVertex]);

            uint32_t newVertexIndex = (uint32_t)outVertices.size() - 1;
            outIndices.push_back(newVertexIndex);

            mapOldToNew[iVertex] = newVertexIndex;
            mapUsedOldVertices.push_back(iVertex);
        }
    }

    // TODO: REMOVE
    auto DEBUG_break_1 = 1;
}

inline void wrapdll::MeshProcessorService::DoMeshIndexingWithTangenSmoothing_OutPutRemap_Fast(const std::vector<PackedCommonVertex>& inVertices, std::vector<PackedCommonVertex>& outVertices, std::vector<uint32_t>& outIndices, std::vector<uint32_t>& mapUsedOldVertices, std::vector<uint32_t>& mapOldToNew)
{
    mapUsedOldVertices.clear(); // can never be too sure?:)                    

    std::map<PackedVertex, uint32_t> VertexToOutIndex;
    mapOldToNew.resize(inVertices.size());
    // For each input vertex
    for (unsigned int iVertex = 0; iVertex < inVertices.size(); iVertex++)
    {
        uint32_t indexToMatchingVertex;

        // TODO: reconfigure to "fast" mapped version
        // TODO: to use this version make sure to have everything but pos, uv, normal = 0
        // 
        // inTangents (flat), outTangents ("smoothed")
        // inBitangents(flat), outBitangents ("smoothed")
        //PackedVertex packedVertex;
        //packedVertex.position = convert::ConvertToVec3(inVertices[inVertexIndex].position);
        //packedVertex.uv = inVertices[inVertexIndex].uv;
        //packedVertex.normal = inVertices[inVertexIndex].normal;
        //
        //bool found = GetSimilarVertexIndex_Fast(packedVertex, VertexToOutIndex, indexToMatchingVertex);

        bool matchingVertexFound = GetSimilarPackedVertexIndex_Slow(convert::ConvertToVec3(inVertices[iVertex].position), inVertices[iVertex].uv, inVertices[iVertex].normal, outVertices, indexToMatchingVertex);

        if (matchingVertexFound) // A similar vertex is already in the new OUTPUT Vertex buffer, use that!
        {
            outIndices.push_back(indexToMatchingVertex); // refer to existing vertex+vertexweight

            // Average the tangents and the bitangents
            outVertices[indexToMatchingVertex].tangent = sm::Vector3(outVertices[indexToMatchingVertex].tangent) + sm::Vector3(inVertices[iVertex].tangent);
            outVertices[indexToMatchingVertex].bitangent = sm::Vector3(outVertices[indexToMatchingVertex].bitangent) + sm::Vector3(inVertices[iVertex].bitangent);

            mapOldToNew[iVertex] = indexToMatchingVertex;


            // TODO:  for better support the faster version, change to
                  // Average the tangents and the bitangents
            //outTangents[indexToMatchingVertex].tangent = sm::Vector3(inTangents[indexToMatchingVertex].tangent) + sm::Vector3(inTangent[iVertex]);
            //outBitangents[indexToMatchingVertex].bitangent = sm::Vector3(inBiTangents[indexToMatchingVertex].bitangent) + sm::Vector3(inBitangents[iVertex]);


        }
        else // No matching vertex found, add a vertex from the INPUT vertex buffer
        {
            outVertices.push_back(inVertices[iVertex]);

            uint32_t newVertexIndex = (uint32_t)outVertices.size() - 1;
            outIndices.push_back(newVertexIndex);

            mapOldToNew[iVertex] = newVertexIndex;
            mapUsedOldVertices.push_back(iVertex);

            // for better support the faster version, change to
            //outTangents[iVertex].push_back(inTangents[iVertex]);
            //outBitangents[iVertex].push_back(inBitangents[iVertex]);
        }
    }

    // TODO: REMOVE
    auto DEBUG_break_1 = 1;
}

inline void wrapdll::MeshProcessorService::indexVBO_TBN_Fast_Packed(const std::vector<PackedCommonVertex>& inVertices, std::vector<uint32_t>& out_indices, std::vector<PackedCommonVertex>& out_vertices) {
    std::map<PackedVertex, uint32_t> VertexToOutIndex;

    // For each input vertex
    for (unsigned int i = 0; i < inVertices.size(); i++)
    {
        PackedVertex packedVertex;
        packedVertex.position = convert::ConvertToVec3(inVertices[i].position);
        packedVertex.uv = inVertices[i].uv;
        packedVertex.normal = inVertices[i].normal;

        // Try to find a similar vertex in out_XXXX
        uint32_t index;

        //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
        //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
        bool found = GetSimilarVertexIndex_Fast(packedVertex, VertexToOutIndex, index);

        if (found) { // A similar vertex is already in the VBO, use it instead !
            out_indices.push_back(index);

            // Average the tangents and the bitangents
            out_vertices[index].tangent = sm::Vector3(out_vertices[index].tangent) + sm::Vector3(inVertices[i].tangent);
            out_vertices[index].bitangent = sm::Vector3(out_vertices[index].bitangent) + sm::Vector3(inVertices[i].bitangent);
        }
        else
        { // If not, it needs to be added in the output data.
            out_vertices.push_back(inVertices[i]);

            uint32_t newindex = (uint32_t)out_vertices.size() - 1;

            out_indices.push_back(newindex);
            VertexToOutIndex[packedVertex] = newindex;
        }
    }
}


// TODO: static duplicate of above, is it needed, IF SO, move it to a "helper" class

inline void wrapdll::MeshProcessorService::ComputeTangentBasisUnindexed(std::vector<PackedCommonVertex>& vertices)
{
    for (size_t i = 0; i < vertices.size(); i += 3)
    {
        // Shortcuts for vertices
        const sm::Vector3& v0 = convert::ConvertToVec3(vertices[i + 0u].position);
        const sm::Vector3& v1 = convert::ConvertToVec3(vertices[i + 1u].position);
        const sm::Vector3& v2 = convert::ConvertToVec3(vertices[i + 2u].position);

        // Shortcuts for UVs
        const sm::Vector2& uv0 = vertices[i + 0u].uv;
        const sm::Vector2& uv1 = vertices[i + 1u].uv;
        const sm::Vector2& uv2 = vertices[i + 2u].uv;

        // Edges of the triangle : postion delta
        sm::Vector3 deltaPos1 = v1 - v0;
        sm::Vector3 deltaPos2 = v2 - v0;

        // UV delta
        sm::Vector2 deltaUV1 = uv1 - uv0;
        sm::Vector2 deltaUV2 = uv2 - uv0;

        float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
        sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
        sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

        vertices[i + 0u].tangent = tangent;
        vertices[i + 1u].tangent = tangent;
        vertices[i + 2u].tangent = tangent;

        vertices[i + 0u].bitangent = bitangent;
        vertices[i + 1u].bitangent = bitangent;
        vertices[i + 2u].bitangent = bitangent;
    }
}

void wrapdll::MeshProcessorService::RemapVertexWeights(
    const std::vector<VertexWeight>& inVertexWeights,
    std::vector<VertexWeight>& outVertexWeights,
    const std::vector<uint32_t>& inMapUsedOldVertices,
    const std::vector<uint32_t>& inMapOldVertexToNew,
    uint32_t vertexCount)
{
    /*
        step 1: only keep the weights with indexes included in "used old vertices"
        steo 2: update "old vertex indexes" to "new vertex indexes"
    */

    outVertexWeights.clear();
    for (size_t iWeight = 0; iWeight < inVertexWeights.size(); iWeight++) // run through all old vertexweights
    {        
        // does the vertex index exist in the "used old vertices" index table?
        if (std::find(
            inMapUsedOldVertices.begin(), 
            inMapUsedOldVertices.end(), 
            inVertexWeights[iWeight].vertexIndex) != inMapUsedOldVertices.end())
        {
            VertexWeight newVertexWeight;
            
            CopyFixedString(newVertexWeight.boneName, inVertexWeights[iWeight].boneName);
            
            // map "old vertex index" to "new vertex index"
            newVertexWeight.vertexIndex = inMapOldVertexToNew[inVertexWeights[iWeight].vertexIndex];
            newVertexWeight.weight = inVertexWeights[iWeight].weight;

            outVertexWeights.push_back(newVertexWeight);
        }
    }    
}

// TODO: Another duplicate?

inline void wrapdll::MeshProcessorService::ComputeTangentBasisIndexed(std::vector<PackedCommonVertex>& vertices, const std::vector<uint32_t>& indices)
{
    // iterate over triangles
    for (size_t faceIndex = 0; faceIndex < indices.size(); faceIndex += 3)
    {
        // Corner index-to-vertices of triangle N
        const auto& cornerIndex0 = indices[faceIndex + 0U];
        const auto& cornerIndex1 = indices[faceIndex + 1U];
        const auto& cornerIndex2 = indices[faceIndex + 2U];

        const sm::Vector3& v0 = convert::ConvertToVec3(vertices[cornerIndex0].position);
        const sm::Vector3& v1 = convert::ConvertToVec3(vertices[cornerIndex1].position);
        const sm::Vector3& v2 = convert::ConvertToVec3(vertices[cornerIndex2].position);

        // Shortcuts for UVs
        const sm::Vector2& uv0 = vertices[cornerIndex0].uv;
        const sm::Vector2& uv1 = vertices[cornerIndex1].uv;
        const sm::Vector2& uv2 = vertices[cornerIndex2].uv;

        // Edges of the triangle : postion delta
        sm::Vector3 deltaPos1 = v1 - v0;
        sm::Vector3 deltaPos2 = v2 - v0;

        // UV delta
        sm::Vector2 deltaUV1 = uv1 - uv0;
        sm::Vector2 deltaUV2 = uv2 - uv0;

        float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
        sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
        sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

        vertices[cornerIndex0].tangent = tangent;
        vertices[cornerIndex1].tangent = tangent;
        vertices[cornerIndex2].tangent = tangent;

        vertices[cornerIndex0].bitangent = bitangent;
        vertices[cornerIndex1].bitangent = bitangent;
        vertices[cornerIndex2].bitangent = bitangent;
    }
}

inline void wrapdll::MeshProcessorService::DoIndexingAndAverageTangents_Slow(const std::vector<sm::Vector3>& inVertices, const std::vector<sm::Vector2>& inUVs, const std::vector<sm::Vector3>& inNormals, const std::vector<sm::Vector3>& inTangents, const std::vector<sm::Vector3>& inBitangents, std::vector<uint32_t>& outIndices, std::vector<sm::Vector3>& outVertices, std::vector<sm::Vector2>& outUVs, std::vector<sm::Vector3>& outNormals, std::vector<sm::Vector3>& outTangents, std::vector<sm::Vector3>& outBitangents) {
    //std::map<ExtPackedCommonVertex, unsigned short> VertexToOutIndex;

    std::vector<int> avg_count/*(inVertices.size(), 1)*/;
    // For each input vertex
    for (unsigned int i = 0; i < inVertices.size(); i++) {
        PackedCommonVertex packed;
        packed.position = sm::Vector4::FromFloat3(inVertices[i]);
        packed.uv = inUVs[i];
        packed.normal = inNormals[i];

        // Try to find a similar vertex in out_XXXX
        uint32_t index;

        //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
        bool found = GetSimilarVertexIndex(inVertices[i], inUVs[i], inNormals[i], outVertices, outUVs, outNormals, index);

        if (found) { // A similar vertex is already in the VBO, use it instead !
            outIndices.push_back(index);

            // Average the tangents and the bitangents, for "smoothing"
            outTangents[index] += inTangents[i];
            outBitangents[index] += inBitangents[i];
        }
        else { // If not, it needs to be added in the output data.
            outVertices.push_back(inVertices[i]);
            outUVs.push_back(inUVs[i]);
            outNormals.push_back(inNormals[i]);
            outTangents.push_back(inTangents[i]);
            outBitangents.push_back(inBitangents[i]);

            uint32_t newindex = (uint32_t)outVertices.size() - 1;

            outIndices.push_back(newindex);
        }
    }
}

