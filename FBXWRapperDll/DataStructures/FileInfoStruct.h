#pragma once
#include <DirectXMath.h>

struct FileInfoStruct
{
    DirectX::XMINT3 sdkVersionUsed = { 0,0,0 };
    char fileName[255] = "";
    char skeletonName[255] = "";
    char units[255] = "";
    float scaleFatorToMeters = 0.0;
    int elementCount = 0;
    int meshCount = 0;
    int materialCount = 0;    
    int animationsCount = 0;
    int boneCount = 0;

};

