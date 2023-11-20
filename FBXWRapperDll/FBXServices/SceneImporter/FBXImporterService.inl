#pragma once

extern "C"
{
	FBXWRAPPERDLL_API_EXT
	wrapdll::FBXImporterService* CreateSceneFBX(char* path)
    {
		return wrapdll::FBXImporterService::CreateFromDiskFile(path);
	};

FBXWRAPPERDLL_API_EXT
	wrapdll::FBXImporterService* CreateFBXSceneImporterService(char* path)
    {
		return new wrapdll::FBXImporterService;
	};

	FBXWRAPPERDLL_API_EXT
	wrapdll::SceneContainer* ProcessAndFillScene(wrapdll::FBXImporterService* pInstance)
	{
		pInstance->ProcessAndFillScene();

		return &pInstance->GetScene();
	};

    // TODO: find out if all this can be removed
   
	//FBXWRAPPERDLL_API_EXT
	//	char* GetSkeletonNameFromScene(wrapdll::FBXImporterService* pInstance, int meshindex)
	//{
	//	return (char*)pInstance->GetSkeletonNameFromSceneNodes();
	//};

	//FBXWRAPPERDLL_API_EXT
	//	void AddBoneName(wrapdll::FBXImporterService* pInstance, char* boneName, int len)
	//{
	//	std::string tempBoneName(boneName, len);
	//	pInstance->GetBoneNames().push_back(tempBoneName);
	//};

	//FBXWRAPPERDLL_API_EXT
	//	void ClearBoneNames(wrapdll::FBXImporterService* pInstance)
	//{
	//	pInstance->GetBoneNames().clear();
	//};

	FBXWRAPPERDLL_API_EXT
		void DeleteBaseObj(wrapdll::BaseInteropObject* pInstance)
	{
		if (pInstance != nullptr)
		{
			delete pInstance;
			pInstance = nullptr;
		}
	};
}
