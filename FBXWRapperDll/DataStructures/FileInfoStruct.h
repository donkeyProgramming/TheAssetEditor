#pragma once
#include <DirectXMath.h>

struct FileInfoStruct
{
    DirectX::XMINT3 sdkVersionUsed = { 1,2,3 };
    char units[255] = "my ass units";
    float scaleFatorToMeters = 0.1234f;
    int elementCount = 200;
    int meshCount = 4;
};

