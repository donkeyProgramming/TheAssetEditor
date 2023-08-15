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
			LogInfo("FBX SDK Manager object deallocated corretcly.");
		};

		/// <summary>
		/// Inits the SDK FbxScene using a 
		/// </summary>		
		static FBXImporterService* CreateFromDiskFile(const std::string& path);

		/// <summary>
		/// Makes and fills the FBXSceneContainer
		/// </summary>		
		FBXSCeneContainer* ProcessAndFillScene();

        /// <summary>
        /// Fille the scene containter file info structure, from the FBX scene/sdk manager
        /// </summary>
        void FillFileInfo();

        // TODO: find out if you can remove

        // TODO: find out if you can remove
		//std::vector<std::string>& GetBoneNames()
		//{
		//	return m_animFileBoneNames;
		//}

		/// <summary>
		/// Get reference to scene containter object
		/// </summary>
		/// <returns>Internal SceneContainer instance</returns>
		FBXSCeneContainer& GetSceneContainer()
		{
			return m_sceneContainer;
		}

	private:
        // TODO: find out, if you can remove
		//std::vector<std::string> m_animFileBoneNames; // ordered as the .ANIM file, so can be used for bonename -> index lookups
		
		FBXSCeneContainer m_sceneContainer; // the container of mesh, anim, etc, that is copied to/from C#, stored here, so no extra pointer cleanup needed

		fbxsdk::FbxScene* m_pFbxScene = nullptr;
		fbxsdk::FbxManager* m_pSDKManager = nullptr;
	};

};


