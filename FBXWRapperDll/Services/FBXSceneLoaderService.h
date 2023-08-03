#pragma once

#include <algorithm>
#include "..\FBXSceneContainer\FBXSCeneContainer.h"

namespace wrapdll
{
	class FBXImporterService : public BaseInteropObject
	{
	public:		
		virtual ~FBXImporterService()
		{			
			m_pSDKManager->Destroy();
			log_info("FBX SDK Manager object deallocated corretcly.");
		};

		/// <summary>
		/// Inits the SDK FbxScene using a 
		/// </summary>		
		static FBXImporterService* CreateFromDiskFile(const std::string& path);

		/// <summary>
		/// Makes and fills the FBXSceneContainer
		/// </summary>		

		FBXSCeneContainer* ProcessAndFillScene();

        void FillFileInfo();

		void FillBoneNames(const std::vector<std::string>& boneNames)
		{
			m_animFileBoneNames = boneNames;
		}
        // TODO: remove?
		/*const char* GetSkeletonNameFromSceneNodes()
		{			
			m_skeletonName = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene);
			return m_skeletonName.c_str();
		}*/

		std::vector<std::string>& GetBoneNames()
		{
			return m_animFileBoneNames;
		}

		FBXSCeneContainer& GetSceneContainer()
		{
			return m_sceneContainer;
		}

	private:
		std::vector<std::string> m_animFileBoneNames; // ordered as the .ANIM file, so can be used for bonename -> index lookups
		
		FBXSCeneContainer m_sceneContainer; // the container of mesh, anim, etc, that is copied to/from C#, stored here, so no extra pointer cleanup needed

		fbxsdk::FbxScene* m_pFbxScene = nullptr;
		fbxsdk::FbxManager* m_pSDKManager = nullptr;
	};

};


