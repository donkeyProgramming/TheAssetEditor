#pragma once

#include <fbxsdk.h>
#include "..\Logging\Logging.h"
#include "..\Helpers\FBXUnitHelper.h"

namespace wrapdll
{
	class FBXHelperFileUtil
	{
	public:
		static fbxsdk::FbxScene* InitScene(fbxsdk::FbxManager* pSdkManager, const std::string path)
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
			log_action("Loading File: \"" + path + "\"");
			auto fileInitResult = poImporter->Initialize(path.c_str(), -1, ios);
			if (!fileInitResult)
			{
				log_action("Error Loading File : \"" + path + " \". Stopping!");
				return nullptr;
			}

			int x=0, y=0, z=0;
			poImporter->GetFileVersion(x, y, z);
			log_action("File uses FBX Version " + std::to_string(x) + "." + std::to_string(y) + "." + std::to_string(z));

			// -- imports the loaded file into the scene!!
			if (!poImporter->Import(pfbxScene))
			{
				log_action_error("Importing scene failed. Fatal Error. Stopping!");
				return nullptr;
			}			

			TransFormScene(pSdkManager, pfbxScene);

			poImporter->Destroy();

			return pfbxScene;
		}

		static void TransFormScene(fbxsdk::FbxManager* m_pSdkManager, fbxsdk::FbxScene* pfbxScene)
		{
			auto unitPlural = FBXUnitHelper::GetUnitAsString(pfbxScene);
			log_info("File Uses Units: " + unitPlural);

			log_write("Importing scene failed. Fatal Error. Stopping!");

			fbxsdk::FbxGeometryConverter geometryConverter(m_pSdkManager);

			log_action("Triangulating....");

			bool bTriangulateResult = geometryConverter.Triangulate(pfbxScene, true, true);

			if (!bTriangulateResult)
			{
				log_action_error("Triangulating Failed! Fatal. Stopping");
				return;
			}

			// perform "deep convert" of everything (including animations), to a certain coord system, that fits our needs
			log_action_success("Performing Deep Conversion of scene to 'DirectX' coord system...");
			fbxsdk::FbxAxisSystem oAxis(fbxsdk::FbxAxisSystem::DirectX);
			oAxis.DeepConvertScene(pfbxScene);
		}

		// Creates an instance of the SDK manager.
		static fbxsdk::FbxManager* InitializeSdkManager()
		{					
			log_action("Initializing FBX SDK importer...");

			// Create the FBX SDK memory manager object.
			// The SDK Manager allocates and frees memory
			// for almost all the classes in the SDK.
			auto pSdkManager = fbxsdk::FbxManager::Create();
			if (!pSdkManager)
			{
				log_action_error("Initializing FBX SDK importer...");
				return nullptr;
			}

			log_action_success("Iinitializing FBX SDK importer...");

			return pSdkManager;
		}

	};
}