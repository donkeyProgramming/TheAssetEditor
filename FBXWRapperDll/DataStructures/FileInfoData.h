#pragma once
#include <DirectXMath.h>
#include <string>

static const size_t fixedStringLength = 256;
using FixedString = char[fixedStringLength];

static void CopyToFixedString(FixedString& output, const std::string& input)
{
    strcpy_s<fixedStringLength>(output, input.c_str());
}

static void CopyToFixedString(FixedString& output, const char* input)
{
    strcpy_s<fixedStringLength>(output, input);
}

static void CopyFromFixedString(std::string& outputString, const FixedString inputFixed)
{  
    outputString = inputFixed;
    outputString.resize(fixedStringLength);
}

static void CopyFixedString(FixedString& outoutFixed, const FixedString& inputFixed)
{  
    strcpy_s<fixedStringLength>(outoutFixed, inputFixed);
}

struct FbxFileInfoData
{
    DirectX::XMINT3 sdkVersionUsed = { 0,0,0 };
    FixedString fileName = "";
    FixedString skeletonName = "";
    FixedString units = "";
    bool isIdStringBone = false;
    float scaleFatorToMeters = 0.0;
    int elementCount = 0;
    int meshCount = 0;
    int materialCount = 0;    
    int animationsCount = 0;
    int boneCount = 0;
    bool containsDerformationData = false;
};

