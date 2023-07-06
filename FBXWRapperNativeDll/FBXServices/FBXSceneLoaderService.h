#pragma once

#include <algorithm>
#include "..\FBXScene\FBXSCeneContainer.h"

namespace wrapdll
{
	class FBXImporterService : public BaseInteropObject
	{
	public:		
		virtual ~FBXImporterService()
		{			
			m_pSDKManager->Destroy();
			log_info("FBX SDK Manager object deallocated.");
		};

		/// <summary>
		/// Inits the SDK FbxScene using a 
		/// </summary>		
		static FBXImporterService* CreateFromDiskFile(const std::string& path);

		/// <summary>
		/// Makes and fills the FBXSceneContainer
		/// </summary>		

		FBXSCeneContainer* ProcessAndFillScene();

		void FillBoneNames(const std::vector<std::string>& boneNames)
		{
			m_animFileBoneNames = boneNames;
		}

		const char* GetSkeletonNameFromSceneNodes()
		{
			// TODO: should copy this to SceneContainer
			m_skeletonName = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene);
			return m_skeletonName.c_str();
		}

		std::vector<std::string>& GetBoneNames()
		{
			return m_animFileBoneNames;
		}

		FBXSCeneContainer& GetSceneContainer()
		{
			return m_sceneContainer;
		}

	private:
		std::string m_skeletonName = "";
		std::vector<std::string> m_animFileBoneNames; // ordered as the .ANIM file, so can be used for bonename -> index lookups
		
		FBXSCeneContainer m_sceneContainer; // the container of mesh, anim, etc, that is copied to/from C#

		fbxsdk::FbxScene* m_pFbxScene = nullptr;
		fbxsdk::FbxManager* m_pSDKManager = nullptr;
	};

};


