#pragma once

#ifdef FBXWRAPPERDLL_EXPORTS
#define FBX_DLL __declspec(dllexport)
#else
#define FBXWRAPPERDLL_API __declspec(dllimport)
#endif

#ifdef FBXWRAPPERDLL_EXPORTS
#define FBXWRAPPERDLL_API_EXT extern "C" __declspec(dllexport)
#else
#define FBXWRAPPERDLL_API_EXT extern "C" __declspec(dllimport)
#endif

