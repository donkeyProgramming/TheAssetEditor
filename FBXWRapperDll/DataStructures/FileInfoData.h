#pragma once
#include <DirectXMath.h>
#include <string>

static const size_t fixedStringLength = 255;
using FixedString = char[fixedStringLength];

static void CopyToFixedString(FixedString& out, const std::string& input)
{
    strcpy_s<fixedStringLength>(out, input.c_str());
}

struct FbxFileInfoData
{
    DirectX::XMINT3 sdkVersionUsed = { 0,0,0 };
    FixedString fileName = "";
    FixedString skeletonName = "";
    FixedString units = "";
    float scaleFatorToMeters = 0.0;
    int elementCount = 0;
    int meshCount = 0;
    int materialCount = 0;    
    int animationsCount = 0;
    int boneCount = 0;
    bool containsDerformationData = false;
};

