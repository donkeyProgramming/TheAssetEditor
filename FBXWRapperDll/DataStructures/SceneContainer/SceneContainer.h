// T-he following ifdef block is the standard way of creating macros which make exporting
// from a DLL slogfunc::impler. All files within this DLL are compiled with the FBXWRAPPERDLL_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// FBXWRAPPERDLL_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#pragma once

#include <fbxsdk.h>
#include <DirectXMath.h>
#include <d3d.h>
#include <Vector>
#include <memory>
#include <string.h>

#include "..\..\Base\BaseInteropObject.h"
#include "..\..\Logging\Logging.h"
#include "..\PackedMeshStructs.h"
#include "..\..\Helpers\FBXHelperFileUtil.h"
#include "..\..\Helpers\Geometry\FBXNodeSearcher.h"
#include "..\FileInfoData.h"
#include "..\..\DLLDefines.h"

namespace wrapdll
{
	class SceneContainer : public BaseInteropObject
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

        uint16_t* AllocateIndices(int meshindex, int indexCount);        
        uint16_t* GetIndices(int meshindex, int* itemCount);

        
        VertexWeight* AllocateVertexWeights(int meshindex, int weightCount);
        void GetVertexWeights(int meshindex, VertexWeight** pVertexWeights, int* itemCount);                   
        
        BoneInfo* AllocateBones(int weightCount);
        void GetBones(BoneInfo** pVertexWeights, int* itemCount);

        void SetIndices(int meshindex, uint16_t* ppIndices, int indexCount);
        void SetVertices(int meshindex, PackedCommonVertex* ppVertices, int vertexCount);
        void SetVertexWeights(int meshindex, VertexWeight* pVertexWeights, int weightCOunt);



        std::vector <PackedMesh>& GetMeshes() {
			return m_packedMeshes;
		};

		std::string& GetSkeletonName()	{
            return m_skeletonName;
		};        	
        
        void SetSkeletonName(const std::string& skeletonName)
        {
            m_skeletonName = skeletonName;
		};        	

		FbxFileInfoData& GetFileInfo()	{                       
			return m_fileInfoStruct;
		};

        FbxFileInfoData m_fileInfoStruct;
          
        // TODO: remove?
		std::string m_skeletonName = "";
		std::vector<PackedMesh> m_packedMeshes;		
        std::vector <BoneInfo> m_bones;

    private:
        bool MeshIndexErrorCheckAndLog(int meshIndex);
	};
}