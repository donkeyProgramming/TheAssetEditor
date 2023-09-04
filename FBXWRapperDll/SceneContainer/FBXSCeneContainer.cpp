#include "FBXSceneContainer.h"

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Processing\MeshProcessor.h"
#include "..\Processing\FBXSkinProcessor.h"
#include "..\Helpers\FBXHelperFileUtil.h"
#include "..\Helpers\Geometry\FBXNodeSearcher.h"


void wrapdll::FBXSCeneContainer::GetVertices(int meshindex, PackedCommonVertex** ppVertices, int* itemCount)
{
    *itemCount = static_cast<int>(m_packedMeshes[meshindex].vertices.size());
    *ppVertices = m_packedMeshes[meshindex].vertices.data();
}

void wrapdll::FBXSCeneContainer::GetIndices(int meshindex, uint16_t** ppIndices, int* itemCount)
{
    *itemCount = static_cast<int>(m_packedMeshes[meshindex].indices.size());
    *ppIndices = m_packedMeshes[meshindex].indices.data();
}

void wrapdll::FBXSCeneContainer::GetVertexWeights(int meshindex, VertexWeight** ppVertices, int* itemCount)
{
    *itemCount = static_cast<int>(m_packedMeshes[meshindex].vertexWeights.size());
    if (*itemCount == 0)
    {
        *ppVertices = nullptr; // STL standard, empty std::vecter.data() not guaranteed to by "nullprt"?
        return;
    }

    *ppVertices = m_packedMeshes[meshindex].vertexWeights.data();
}

void wrapdll::FBXSCeneContainer::AllocateMeshes(int meshCount)
{
    m_packedMeshes.clear();
    m_packedMeshes.resize(meshCount);
}
;

// dll externs
#include "FBXSceneContainer.inl"