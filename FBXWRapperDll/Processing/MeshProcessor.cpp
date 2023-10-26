#include "MeshProcessor.h"


//TODO: use meshopt, and add VertexRemapper variant of their code, that takes "CommonPackedVertex*"
// And processes the the tangents like in "DoMeshIndexingWithTangentSmoothing"
// Their hashing method is proper 10% faster, and it it MIT license 


// TODO: maybe also use this code, for meshes before Saving them as FBX?

void wrapdll::MeshProcessor::DoFinalMeshProcessing(PackedMesh& mesh)
{
    ComputeTangentBasisUnindexed(mesh.vertices);
    DoMeshIndexingWithTangentSmoothing(mesh);
}

void wrapdll::MeshProcessor::DoTangentBasisAndIndexing(PackedMesh& destMesh)
{
    using namespace std;
    using namespace DirectX;

    // -- input
    vector<sm::Vector3> inVertices;
    vector<sm::Vector2> inUVs;
    vector<sm::Vector3> inNormals;

    vector<sm::Vector3> outVertices;
    vector<sm::Vector2> outUVs;
    vector<sm::Vector3> outNormals;
    vector<sm::Vector3> outTangents;
    vector<sm::Vector3> outBitangents;

    vector<uint32_t> outIndices;

    for (auto& v : destMesh.vertices)  // fill the UN-INDEXED vertex data into vectors
    {
        inVertices.push_back({ v.position.x, v.position.y, v.position.z }); // init: input uses vector4 for position
        inUVs.push_back(v.uv);
        inNormals.push_back(v.normal);
    };    

    ComputeTangentBasisForUnindexedMesh(
        // inputs
        inVertices, inUVs, inNormals,

        // outputs	
        outTangents, outBitangents
    );

    // do indexing  and average tangents
    DoIndexingAndAverageTangents_Slow(
        inVertices, inUVs, inNormals, outTangents, outBitangents,
        outIndices,
        outVertices,
        outUVs,
        outNormals,
        outTangents,
        outBitangents
    );

    // -- fill the mesh with the proceessed data
    destMesh.vertices.clear();
    destMesh.vertices.resize(outVertices.size());

    for (size_t i = 0; i < outVertices.size(); i++)
    {
        auto& v = destMesh.vertices[i];
        auto& v_src = destMesh.vertices[i];
        v.position = XMFLOAT4(outVertices[i].x, outVertices[i].y, outVertices[i].z, 0);

        outNormals[i].Normalize();
        v.normal = outNormals[i];

        outTangents[i].Normalize();
        outBitangents[i].Normalize();

        v.tangent = outTangents[i];
        v.bitangent = outBitangents[i];

        v.uv = outUVs[i];
    }

    destMesh.indices = outIndices;
}

void wrapdll::MeshProcessor::RemapVertexWeights(const std::vector<VertexWeight>& inVertexWeights, std::vector<VertexWeight>& outVertexWeights, const std::vector<int>& outVertexIndexRemap)
{
    for (size_t i = 0; i < inVertexWeights.size(); i++) // run through all old vertexweights
    {
        if (outVertexIndexRemap[inVertexWeights[i].vertexIndex] != VERTEX_DISCARDED)
        {
            auto tempVertexWeight = inVertexWeights[i];

            // remap the index, 
            tempVertexWeight.vertexIndex = outVertexIndexRemap[inVertexWeights[i].vertexIndex];

            outVertexWeights.push_back(tempVertexWeight);
        }
    }
}