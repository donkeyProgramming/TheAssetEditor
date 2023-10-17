#pragma once
#include "..\..\fbxsdk\common\Common.h"

extern "C" // not really needed but block looks nice/readable:)
{
    FBX_DLL
    wrapdll::FBXSceneExporterService* MakeEmptyExporter()
    {
        return wrapdll::FBXSceneExporterService::MakeEmptyExporter();
    };

    FBX_DLL
    wrapdll::SceneContainer* GetNativeSceneContainer(wrapdll::FBXSceneExporterService* pExporter)
    {
        return &pExporter->GetScene();
    };

    FBX_DLL
    void SaveToDisk(wrapdll::FBXSceneExporterService* pExporter, const char* szPath)
    {
        pExporter->SaveToDisk(szPath);
    };
}