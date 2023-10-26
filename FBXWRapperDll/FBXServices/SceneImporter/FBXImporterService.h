#pragma once

#include <algorithm>
#include <fbxsdk.h>

#include "..\..\DataStructures\SceneContainer\SceneContainer.h"

namespace wrapdll
{
	class FBXImporterService : public BaseInteropObject
	{        
	public:		
		virtual ~FBXImporterService()
		{			
			m_pSDKManager->Destroy();
			LogInfo("FBX SDK Manager object deallocated correctly.");
		};

		/// <summary>
		/// Inits the SDK FbxScene using a 
		/// </summary>		
		static FBXImporterService* CreateFromDiskFile(const std::string& path);

		/// <summary>
		/// Makes and fills the FBXSceneContainer
		/// </summary>		
		SceneContainer* ProcessAndFillScene();

        /// <summary>
        /// Fille the scene containter file info structure, from the FBX scene/sdk manager
        /// </summary>
        void FillFileInfo();      

		/// <summary>
		/// Get reference to scene containter object
		/// </summary>
		/// <returns>Internal SceneContainer instance</returns>
		SceneContainer& GetScene()
		{
			return m_sceneContainer;
		}

	private:
		SceneContainer m_sceneContainer; // the container of mesh, anim, etc, that is copied to/from C#, stored here, so no extra pointer cleanup needed

		fbxsdk::FbxScene* m_pFbxScene = nullptr;
		fbxsdk::FbxManager* m_pSDKManager = nullptr;
	};

};


