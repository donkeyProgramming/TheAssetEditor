#include "MeshProcessorService.h"
#include "..\libs\meshopt\meshoptimizer.h"
#include "..\libs\MS_SimpleMath\SimpleMath.h"

//TODO: use meshopt, and add VertexRemapper variant of their code, that takes "CommonPackedVertex*"
// And processes the the tangents like in "DoMeshIndexingWithTangentSmoothing"
// Their hashing method is proper 10% faster, and it it MIT license 


// TODO: maybe also use this code, for meshes before Saving them as FBX?

void wrapdll::MeshProcessorService::DoFinalProcessing()
{
    //Original
    //ComputeTangentBasisUnindexed(m_poDestMesh->vertices);
    //DoMeshIndexingWithTangentSmoothing();

    ComputeFaceTangentsUnindexed();
    DoMeshIndexingWithTangentSmoothing();

    //// TODO: cleanup method, once it works
    //auto timedLogger = TimeLogAction::PrintStart("Do tangentBasis and indexing");

    //DoTangentBasisAndIndexing();

    //timedLogger.PrintDone();
}

void wrapdll::MeshProcessorService::DoTangentBasisAndIndexing_MeshOpt()
{
    // TODO: cleanup
    using namespace std;
    using namespace DirectX;

    // - comput "flat" tangents, the same for each corner of the face
    ComputeFaceTangentsUnindexed();

    // TODO: , this is not reducing indexes meshes enough because, it is use the whole PackedCommonVertex"

    // - unindexes mesh
    auto indexCount = m_poDestMesh->indices.size();

    // -- Make vertices, that contain less data, so the vertex remapper can detect more "indentical vertices"    
    std::vector<PackedCommonVertex> tempVertices(m_poDestMesh->vertices.size());
    for (size_t i = 0; i < m_poDestMesh->vertices.size(); i++)
    {
        tempVertices[i].position = m_poDestMesh->vertices[i].position;
        tempVertices[i].normal = m_poDestMesh->vertices[i].normal;
        tempVertices[i].uv = m_poDestMesh->vertices[i].uv;
    }

    // -- Makes an 
    vector<unsigned int> remapTable(tempVertices.size());
    auto newVertexCount = meshopt_generateVertexRemapTBN(
        remapTable.data(),
        &m_poDestMesh->indices[0],
        m_poDestMesh->indices.size(),
        tempVertices.data(),
        tempVertices.size(),
        sizeof(PackedCommonVertex)
    );

    // TODO: REMOVE, this is for comparison
    //vector<uint32_t> vertexRemap_OLD(tempVertices.size());
    //auto newVertexCount_Old = meshopt_generateVertexRemapTBN(
    //    remapTable.data(),
    //    &m_destMesh.indices[0],
    //    m_destMesh.indices.size(),
    //    tempVertices.data(),
    //    tempVertices.size(),
    //    sizeof(PackedCommonVertex)
    //);    
    //
    //

    // -- remap index buffer, to fit the new reduced vertex count, and with no indices to discard verticie (index buffer LENGTH is not reduced here)    
    meshopt_remapIndexBuffer(m_poDestMesh->indices.data(), NULL, m_poDestMesh->indices.size(), remapTable.data());

    // -- remap the vertex buffer, so the used vertices are sequential 
    tempVertices.resize(newVertexCount);
    meshopt_remapVertexBuffer(
        tempVertices.data(),
        tempVertices.data(),
        tempVertices.size(),
        sizeof(PackedCommonVertex),
        remapTable.data());

    // -- Put the "smoothed" TBN tangents into the vertices
    m_poDestMesh->vertices.resize(newVertexCount);
    for (size_t i = 0; i < m_poDestMesh->vertices.size(); i++)
    {
        m_poDestMesh->vertices[i].position = tempVertices[i].position;
        m_poDestMesh->vertices[i].normal = tempVertices[i].normal;
        m_poDestMesh->vertices[i].uv = tempVertices[i].uv;
        m_poDestMesh->vertices[i].tangent = tempVertices[i].tangent;
        m_poDestMesh->vertices[i].bitangent = tempVertices[i].bitangent;
    }

    // TODO: if rigging but persists check this for bugs
    // remap the weights to fit the new the reduced mesh
    RemapVertexWeights_MeshOpt(m_poDestMesh->vertexWeights, m_poDestMesh->vertexWeights, remapTable);

    // TODO: remove below?
    //for (size_t i = 0; i < m_destMesh.vertices.size(); i++)
    //{
    //    auto& v = m_destMesh.vertices[i];
    //    auto& v_src = m_destMesh.vertices[i];
    //    v.position = XMFLOAT4(outVertices[i].x, outVertices[i].y, outVertices[i].z, 0);

    //    outNormals[i].Normalize();
    //    v.normal = outNormals[i];

    //    outTangents[i].Normalize();
    //    outBitangents[i].Normalize();

    //    v.tangent = outTangents[i];
    //    v.bitangent = outBitangents[i];

    //    v.uv = outUVs[i];
    //}

    //m_destMesh.indices = outIndices;
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

    auto DEBUG_break = 1;
}


void wrapdll::MeshProcessorService::RemapVertexWeights_MeshOpt(const std::vector<VertexWeight>& inVertexWeights, std::vector<VertexWeight>& outVertexWeights, const std::vector<unsigned int>& outVertexIndexRemap)
{
    throw std::exception("Not implemented");

    std::vector<VertexWeight> tempOutVertexWeights; // support in / out being the same vector



    std::map<unsigned int, unsigned int> vertexIndexRemapLookUp;
    for (unsigned int i = 0; i < outVertexIndexRemap.size(); i++)
    {
        vertexIndexRemapLookUp[i] = outVertexIndexRemap[i];
    }


    for (size_t vWeightIndex = 0; vWeightIndex < inVertexWeights.size(); vWeightIndex++) // run through all old vertexweights
    {

        // does the vertex index exist in the remap table?
        if (vertexIndexRemapLookUp.find(inVertexWeights[vWeightIndex].vertexIndex) != vertexIndexRemapLookUp.end())
        {
            /*if (outVertexIndexRemap[inVertexWeights[vWeightIndex].vertexIndex] != DISCARD_VALUE)
            {*/
            auto tempVertexWeight = inVertexWeights[vWeightIndex];

            // remap the index, 
            tempVertexWeight.vertexIndex = outVertexIndexRemap[tempVertexWeight.vertexIndex];

            // store the weight
            tempOutVertexWeights.push_back(tempVertexWeight);
            //}

        }
    }

    outVertexWeights = tempOutVertexWeights;

}





