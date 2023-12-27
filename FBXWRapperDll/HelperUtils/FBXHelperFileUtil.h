#pragma once

#include <fbxsdk.h>
#include "..\Logging\Logging.h"
#include "..\HelperUtils\FBXUnitHelper.h"

namespace wrapdll
{
	class FBXHelperFileUtil
	{
	public:
        static fbxsdk::FbxScene* CreateFbxScene(fbxsdk::FbxManager* poSdkManager)
        {
            //Create an FBX scene. This object holds most objects imported/exported from/to files.
            auto poFbxScene = FbxScene::Create(poSdkManager, "My Scene");
            if (!poFbxScene)
            {
                FBXSDK_printf("Error: Unable to create FBX scene!\n");
                exit(1);
                return nullptr;
            }

            return poFbxScene;
        }

        static fbxsdk::FbxScene* InitSceneFromFile(fbxsdk::FbxManager* pSdkManager, const std::string path, DirectX::XMINT3* pFileSdkVersion = nullptr)
		{			
			// create an empty scene
			auto pfbxScene = fbxsdk::FbxScene::Create(pSdkManager, "");

			// Create an importer.
			auto poImporter = fbxsdk::FbxImporter::Create(pSdkManager, "");

			// create an IOSettings object
			fbxsdk::FbxIOSettings* ios = fbxsdk::FbxIOSettings::Create(pSdkManager, IOSROOT);

			// set some IOSettings options
			ios->SetBoolProp(IMP_FBX_PIVOT, true);
			ios->SetBoolProp(IMP_FBX_MATERIAL, true);
			//ios->SetBoolProp(IMP_FBX_TEXTURE, true);
			ios->SetBoolProp(IMP_FBX_SHAPE, true);
			ios->SetBoolProp(IMP_FBX_GOBO, true);
			ios->SetBoolProp(IMP_FBX_ANIMATION, true);
			ios->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, true);

			// Initialize the importer by providing a filename and the IOSettings to use
			LogAction("Loading File: \"" + path + "\"");
			auto fileInitResult = poImporter->Initialize(path.c_str(), -1, ios);
			if (!fileInitResult)
			{
				LogAction("Error Loading File : \"" + path + " \". Stopping!");
				return nullptr;
			}

            
			int x=0, y=0, z=0;
			poImporter->GetFileVersion(x, y, z);

            if (pFileSdkVersion)
            {
                *pFileSdkVersion = { x,y,z };
            }

			LogAction("File uses FBX Version " + std::to_string(x) + "." + std::to_string(y) + "." + std::to_string(z));

			// -- imports the loaded file into the scene!!
			if (!poImporter->Import(pfbxScene))
			{
				LogActionError("Importing scene failed. Fatal Error. Stopping!");
				return nullptr;
			}			

			TransFormScene(pSdkManager, pfbxScene);

			poImporter->Destroy();

			return pfbxScene;
		}

        static fbxsdk::FbxScene* InitEmptyScene(fbxsdk::FbxManager* pSdkManager)
        {
            // create an empty scene
            auto pfbxScene = fbxsdk::FbxScene::Create(pSdkManager, "ExportScene");

            return pfbxScene;
        }

		static void TransFormScene(fbxsdk::FbxManager* m_pSdkManager, fbxsdk::FbxScene* pfbxScene)
		{
			auto unitPlural = FBXUnitHelper::GetUnitAsString(pfbxScene);
			LogInfo("File Uses Units: " + unitPlural);			

			fbxsdk::FbxGeometryConverter geometryConverter(m_pSdkManager);

			LogAction("Triangulating....");

			bool bTriangulateResult = geometryConverter.Triangulate(pfbxScene, true, true);

			if (!bTriangulateResult)
			{
				LogActionError("Triangulating Failed! Fatal. Stopping");
				return;
			}

			// perform "deep convert" of everything (including animations), to a certain coord system, that fits our needs
			LogActionSuccess("Performing Deep Conversion of scene to 'DirectX' coord system...");
			fbxsdk::FbxAxisSystem oAxis(fbxsdk::FbxAxisSystem::DirectX);
			oAxis.DeepConvertScene(pfbxScene);
		}

		// Creates an instance of the SDK manager.
		static fbxsdk::FbxManager* InitializeSdkManager()
		{					
			LogAction("Initializing FBX SDK importer...");

            
            
			// Create the FBX SDK memory manager object.
			// The SDK Manager allocates and frees memory
			// for almost all the classes in the SDK.
			auto pSdkManager = fbxsdk::FbxManager::Create();
			if (!pSdkManager)
			{
				LogActionError("Initializing FBX SDK importer...");
				return nullptr;
			}

			LogActionSuccess("Iinitializing FBX SDK importer...");

			return pSdkManager;
		}

	};
}