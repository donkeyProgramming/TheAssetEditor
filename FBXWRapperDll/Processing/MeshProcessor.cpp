#include "MeshProcessor.h"

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
    vector<SimpleMath::Vector3> inVertices;
    vector<SimpleMath::Vector2> inUVs;
    vector<SimpleMath::Vector3> inNormals;

    vector<DirectX::SimpleMath::Vector3> outVertices;
    vector<DirectX::SimpleMath::Vector2> outUVs;
    vector<DirectX::SimpleMath::Vector3> outNormals;
    vector<DirectX::SimpleMath::Vector3> outTangents;
    vector<DirectX::SimpleMath::Vector3> outBitangents;

    vector<uint16_t> outIndices;

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