#pragma once

#include <algorithm>
#include <fbxsdk.h>

#include "../../DataStructures\SceneContainer\SceneContainer.h"
#include "../../fbxsdk\common\Common.h"
#include "../../Dll/DLLDefines.h"
#include "../../Processing\FBXMeshProcessor.h"
#include "../../FbxObjectCreators/FBXSkeletonFactory.h"
#include "../../FbxObjectCreators/FbxSceneCreator.h"


namespace wrapdll
{
    class FBXSceneExporterService : public BaseInteropObject
    {     
    public:
        virtual ~FBXSceneExporterService()
        {
            m_poSDKManager->Destroy();
            LogActionSuccess("FBX SDK Manager object deallocated correctly.");
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
            
            // TODO:change a bit, so FbxScene::Create happens in FbxSceneCreator, 
            //      and FbxSceneCreator::CreateFromSceneContainer take a FbxManager as params, 
            //      and returns the FbxScene*
            InitializeSDKManager(m_poSDKManager);

            FbxSceneCreator sceneCreator;
            auto m_poFbxScene = sceneCreator.CreateFbxScene(m_poSDKManager, m_sceneContainer);

            auto lResult = SaveScene(m_poSDKManager, m_poFbxScene, szDiskPath);

            if (lResult == false)
            {
                FBXSDK_printf("\n\nAn error occurred while saving the scene...\n");
                DestroySdkObjects(m_poSDKManager, lResult);
                return false;
            }

            return true;
        }        
                
        SceneContainer& GetScene()
        {
            return m_sceneContainer;
        }         


        static void InitializeSDKManager(FbxManager*& pManager)
        {
            //The first thing to do is to create the FBX Manager which is the object allocator for almost all the classes in the SDK
            pManager = FbxManager::Create();
            if (!pManager)
            {
                FBXSDK_printf("Error: Unable to create FBX Manager!\n");
                exit(1);
            }
            else FBXSDK_printf("Autodesk FBX SDK version %s\n", pManager->GetVersion());

            //Create an IOSettings object. This object holds all import/export settings.
            FbxIOSettings* ios = FbxIOSettings::Create(pManager, IOSROOT);
            pManager->SetIOSettings(ios);

            //Load plugins from the executable directory (optional)
            FbxString lPath = FbxGetApplicationDirectory();
            pManager->LoadPluginsDirectory(lPath.Buffer());
        }


        
    private:
        SceneContainer m_sceneContainer = SceneContainer();
        fbxsdk::FbxManager* m_poSDKManager = nullptr;
        fbxsdk::FbxScene* m_poFbxScene = nullptr;
    };



}
