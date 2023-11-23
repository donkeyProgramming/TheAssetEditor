#pragma once

extern "C" // not really needed but block looks nice/readable:)
{
    FBX_DLL_WRAP
    wrapdll::FBXSceneExporterService* MakeEmptyExporter()
    {
        return wrapdll::FBXSceneExporterService::MakeEmptyExporter();
    };
    
    FBX_DLL_WRAP
    wrapdll::SceneContainer* GetNativeSceneContainer(wrapdll::FBXSceneExporterService* pExporter)
    {
        return &pExporter->GetScene();
    };

    FBX_DLL_WRAP
    void SaveToDisk(wrapdll::FBXSceneExporterService* pExporter, const char* szPath)
    {
        pExporter->SaveToDisk(szPath);
    };
}