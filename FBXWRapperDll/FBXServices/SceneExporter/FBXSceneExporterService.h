#pragma once

#include <algorithm>
#include "..\..\DataStructures\SceneContainer\SceneContainer.h"
#include "..\..\fbxsdk\common\Common.h"
#include "..\..\DLLDefines.h"
#include "..\..\Processing\FbxMeshCreator.h"
#include "..\..\Processing\FbxMeshCreator.h"
#include "..\..\Builders\FBXSkeletonBuilder.h"

namespace wrapdll
{
    class FBXSceneExporterService : public BaseInteropObject
    {     
    public:
        virtual ~FBXSceneExporterService()
        {
            //m_pSDKManager->Destroy();
            //LogInfo("FBX SDK Manager object deallocated correctly.");
        };


        static FBXSceneExporterService* MakeEmptyExporter()
        {
            auto poNewExporterService = new wrapdll::FBXSceneExporterService();                        

            return poNewExporterService;
        }       

        std::vector<uint8_t> SaveToMemory()
        {
             // TODO: convert the content of ScencContainer to FBX 
        }

        bool SaveToDisk(const char* szDiskPath)
        {
            using namespace fbxsdk;                        
            
            InitializeSdkObjects(m_poSDKManager, m_poFbxScene);

            FBXUnitHelper::SetFbxSystmedUnit(m_poFbxScene, fbxsdk::FbxSystemUnit::Inch);
            
            auto vertexScaleFactor = FBXUnitHelper::GetVertexDataScaleFactor(m_poFbxScene);

            AddMeshes(vertexScaleFactor);

            FBXSkeletonBuilder::FillSkeleton(m_poFbxScene, m_sceneContainer.m_bones);         




            auto lResult = SaveScene(m_poSDKManager, m_poFbxScene, szDiskPath);
            if (lResult == false)
            {
                FBXSDK_printf("\n\nAn error occurred while saving the scene...\n");
                DestroySdkObjects(m_poSDKManager, lResult);
                return true;
            }
            return false;
        }

        void AddMeshes(double vertexScaleFactor)
        {
            for (auto& inPackedMesh : m_sceneContainer.m_packedMeshes)
            {
                auto poMesh = FbxMeshCreator::CreateFbxUnindexedMesh(m_poFbxScene, inPackedMesh, vertexScaleFactor);
                auto poMeshNode = fbxsdk::FbxNode::Create(m_poFbxScene, inPackedMesh.meshName.c_str());
                poMeshNode->SetNodeAttribute(poMesh);

                m_poFbxScene->GetRootNode()->AddChild(poMeshNode);
            }
        }
        ;

        /// <summary>
        /// Get reference to scene containter object
        /// </summary>
        /// <returns>Internal SceneContainer instance</returns>
        SceneContainer& GetScene()
        {
            return m_sceneContainer;
        }
    
    
    
        SceneContainer m_sceneContainer = SceneContainer();
    private:
        fbxsdk::FbxManager* m_poSDKManager = nullptr;
        fbxsdk::FbxScene* m_poFbxScene = nullptr;
    };
}
