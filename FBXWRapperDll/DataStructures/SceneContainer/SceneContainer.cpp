#include "SceneContainer.h"

#include "..\..\DataStructures\PackedMeshStructs.h"
#include "..\..\Processing\MeshProcessor.h"
#include "..\..\Processing\FBXSkinProcessor.h"
#include "..\..\HelperUtils\FBXHelperFileUtil.h"
#include "..\..\HelperUtils\Geometry\FBXNodeSearcher.h"

PackedCommonVertex* wrapdll::SceneContainer::GetVertices(int meshindex, int* itemCount)
{
    if (!MeshIndexErrorCheckAndLog(meshindex)) { return nullptr; }

    *itemCount = static_cast<int>(m_packedMeshes[meshindex].vertices.size());
    return m_packedMeshes[meshindex].vertices.data();
}

uint32_t* wrapdll::SceneContainer::GetIndices(int meshindex, int* itemCount)
{
    if (!MeshIndexErrorCheckAndLog(meshindex)) { return nullptr; }

    *itemCount = static_cast<int>(m_packedMeshes[meshindex].indices.size());
    return m_packedMeshes[meshindex].indices.data();
}



void wrapdll::SceneContainer::GetVertexWeights(int meshindex, VertexWeight** ppVertices, int* itemCount)
{
    if (!MeshIndexErrorCheckAndLog(meshindex)) { return; }

    *itemCount = static_cast<int>(m_packedMeshes[meshindex].vertexWeights.size());
    if (*itemCount == 0)
    {
        *ppVertices = nullptr; // STL standard, empty std::vecter.data() not guaranteed to by "nullprt"?
        return;
    }

    *ppVertices = m_packedMeshes[meshindex].vertexWeights.data();
}



BoneInfo* wrapdll::SceneContainer::AllocateBones(int boneCount)
{
    m_skeletonInfo.m_bones.clear();
    m_skeletonInfo.m_bones.resize(boneCount);

    return m_skeletonInfo.m_bones.data();
}

void wrapdll::SceneContainer::AllocateMeshes(int meshCount)
{
    m_packedMeshes.clear();
    m_packedMeshes.resize(meshCount);
}

VertexWeight* wrapdll::SceneContainer::AllocateVertexWeights(int meshIndex, int weightCount)
{
    if (!MeshIndexErrorCheckAndLog(meshIndex)) { return nullptr; }

    m_packedMeshes[meshIndex].vertexWeights.clear();
    m_packedMeshes[meshIndex].vertexWeights.resize(weightCount);

    return m_packedMeshes[meshIndex].vertexWeights.data();
}

PackedCommonVertex* wrapdll::SceneContainer::AllocateVertices(int meshIndex, int vertexCount)
{
    if (!MeshIndexErrorCheckAndLog(meshIndex)) { return nullptr; }

    m_packedMeshes[meshIndex].vertices.clear();
    m_packedMeshes[meshIndex].vertices.resize(vertexCount);

    return m_packedMeshes[meshIndex].vertices.data();
}

uint32_t* wrapdll::SceneContainer::AllocateIndices(int meshIndex, int indexCount)
{
    if (!MeshIndexErrorCheckAndLog(meshIndex)) { return nullptr; }

    m_packedMeshes[meshIndex].indices.clear();
    m_packedMeshes[meshIndex].indices.resize(indexCount);

    return m_packedMeshes[meshIndex].indices.data();
}

void wrapdll::SceneContainer::SetIndices(int meshIndex, uint32_t* ppIndices, int indexCount)
{
    if (!MeshIndexErrorCheckAndLog(meshIndex)) { return; }

    m_packedMeshes[meshIndex].indices.clear();
    m_packedMeshes[meshIndex].indices.resize(indexCount);

    memcpy(m_packedMeshes[meshIndex].indices.data(), ppIndices, indexCount * sizeof(ppIndices));
}

void wrapdll::SceneContainer::SetVertices(int meshindex, PackedCommonVertex* pVertices, int vertexCount)
{
    if (!MeshIndexErrorCheckAndLog(meshindex)) { return; }

    m_packedMeshes[meshindex].vertices.clear();
    m_packedMeshes[meshindex].vertices.resize(vertexCount);

    memcpy(m_packedMeshes[meshindex].vertices.data(), pVertices, vertexCount * sizeof(*pVertices));
}


bool wrapdll::SceneContainer::MeshIndexErrorCheckAndLog(int meshIndex)
{
    if (m_packedMeshes.size() < meshIndex)
    {
        LogActionError("Invalid Index, mesh count: " + std::to_string(m_packedMeshes.size()) + ", index: " + std::to_string(meshIndex));
        return false;
    }

    return true;
}
//void wrapdll::FbxSceneContainer::SetVertexWeights(int meshIndex, VertexWeight* pVertexWeights, int weightCount)
//{
//    if (meshIndex >= m_packedMeshes.size())
//    {
//        LogActionError("Invalid Index. Index >= Mesh Count ")
//            return;
//    }
//
//    auto pMesh = &m_packedMeshes[meshIndex];
//
//    pMesh->vertexWeights.clear();
//    pMesh->vertexWeights.resize(weightCount);
//
//    pIn
//
//    /*for (size_t i = 0; i < weightCount; i++)
//    {
//        pMesh->vertexWeights[i] = pVertexWeights[i];
//    }*/
//}


// dll externs
#include "SceneContainer.inl"