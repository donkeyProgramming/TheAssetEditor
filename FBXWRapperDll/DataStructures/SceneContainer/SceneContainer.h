#pragma once

#include <Vector>
#include <fbxsdk.h>

#include "..\..\Base\BaseInteropObject.h"
#include "..\..\Logging\Logging.h"
#include "..\PackedMeshStructs.h"
#include "..\FileInfoData.h"
#include "..\..\Dll\DLLDefines.h"
#include <SimpleMath.h>

class IVadlidator
{
public:
    virtual bool IsValid() const = 0;
};

namespace wrapdll
{
    // TODO: maybe replace the "loose" skeleton info in "SceneContainer" with this
    struct SkeletonInfo : public IVadlidator
    {
        std::string m_skeletonName = "";
        std::vector <BoneInfo> m_bones;
        std::vector<fbxsdk::FbxNode*> m_fbxBoneNodes;

        bool IsValid() const override
        {
            return m_skeletonName != "" && m_bones.size();
        }
    };

    class SceneContainer : public BaseInteropObject, IVadlidator
    {
    public:
        virtual ~SceneContainer()
        {
#ifdef _DEBUG
            LogInfo("FBXSCeneContainer destroyed.");
#endif // _DEBUG			
        };

        void AllocateMeshes(int count);

        PackedCommonVertex* AllocateVertices(int meshIndex, int vertexCount);
        PackedCommonVertex* GetVertices(int meshindex, int* itemCount);

        uint32_t* AllocateIndices(int meshindex, int indexCount);
        uint32_t* GetIndices(int meshindex, int* itemCount);

        VertexWeight* AllocateVertexWeights(int meshindex, int weightCount);
        void GetVertexWeights(int meshindex, VertexWeight** pVertexWeights, int* itemCount);

        BoneInfo* AllocateBones(int weightCount);
        void GetBones(BoneInfo** pVertexWeights, int* itemCount);

        void SetIndices(int meshindex, uint32_t* ppIndices, int indexCount);
        void SetVertices(int meshindex, PackedCommonVertex* ppVertices, int vertexCount);
        void SetVertexWeights(int meshindex, VertexWeight* pVertexWeights, int weightCOunt);

        std::vector <PackedMesh>& GetMeshes() { return m_packedMeshes; };
        const std::vector <PackedMesh>& GetMeshes() const { return m_packedMeshes; };

        std::vector <BoneInfo>& GetBones() { return m_skeletonInfo.m_bones; };
        const std::vector <BoneInfo>& GetBones() const { return m_skeletonInfo.m_bones; };

        std::vector<fbxsdk::FbxNode*>& GetFbxBoneNodes() { return m_skeletonInfo.m_fbxBoneNodes; };
        const std::vector<fbxsdk::FbxNode*>& GetFbxBoneNodes() const { return m_skeletonInfo.m_fbxBoneNodes; };

        std::string& GetSkeletonName() { return m_skeletonInfo.m_skeletonName; };
        const std::string& GetSkeletonName() const { return m_skeletonInfo.m_skeletonName; };

        void SetSkeletonName(const std::string& skeletonName)
        {
            m_skeletonInfo.m_skeletonName = skeletonName;
        };

        FbxFileInfoData& GetFileInfo()
        {
            return m_fileInfoStruct;
        };

        double GetDistanceScaleFactor()
        {
            return m_processingDistanceScaleFactory;
        };

        void SetDistanceScaleFactor(double value)
        {
            m_processingDistanceScaleFactory = value;
        };
        
        bool IsValid() const override { 
            return m_packedMeshes.size() > 0; 
        }        

        bool HasSkeleton() const { 
            return m_skeletonInfo.IsValid(); 
        };

    private:
        bool MeshIndexErrorCheckAndLog(int meshIndex);

    private:
        FbxFileInfoData m_fileInfoStruct;        
        std::vector<PackedMesh> m_packedMeshes;
        SkeletonInfo m_skeletonInfo;
        double m_processingDistanceScaleFactory = 1.0;
    };

}