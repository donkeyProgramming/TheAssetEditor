#pragma once

#include <Windows.h>
#include <fstream>
#include <string>
#include <iostream>
#include <fstream>
#include <locale>
#include <codecvt>

static void WriteToLogFile(const std::string& logString)
{
	std::ofstream oOutFile(L"fbxsdk.log.txt", std::ios::app);
	oOutFile << logString;
	oOutFile.close();
}

static std::wstring WidenStr(const std::string& str)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.from_bytes(str);
}

static std::string NarrowStr(const std::wstring& wstr)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.to_bytes(wstr);
}

namespace ConsoleForeground
{
	enum {
		BLACK = 0,
		DARKBLUE = FOREGROUND_BLUE,
		DARKGREEN = FOREGROUND_GREEN,
		DARKCYAN = FOREGROUND_GREEN | FOREGROUND_BLUE,
		DARKRED = FOREGROUND_RED,
		DARKMAGENTA = FOREGROUND_RED | FOREGROUND_BLUE,
		DARKYELLOW = FOREGROUND_RED | FOREGROUND_GREEN,
		DARKGRAY = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
		GRAY = FOREGROUND_INTENSITY,
		BLUE = FOREGROUND_INTENSITY | FOREGROUND_BLUE,
		GREEN = FOREGROUND_INTENSITY | FOREGROUND_GREEN,
		CYAN = FOREGROUND_INTENSITY | FOREGROUND_GREEN | FOREGROUND_BLUE,
		RED = FOREGROUND_INTENSITY | FOREGROUND_RED,
		MAGENTA = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_BLUE,
		YELLOW = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN,
		WHITE = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
	};
}

namespace ConsoleBackground
{
	enum ConsoleBackground {
		BLACK = 0,
		DARKBLUE = BACKGROUND_BLUE,
		DARKGREEN = BACKGROUND_GREEN,
		DARKCYAN = BACKGROUND_GREEN | BACKGROUND_BLUE,
		DARKRED = BACKGROUND_RED,
		DARKMAGENTA = BACKGROUND_RED | BACKGROUND_BLUE,
		DARKYELLOW = BACKGROUND_RED | BACKGROUND_GREEN,
		DARKGRAY = BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
		GRAY = BACKGROUND_INTENSITY,
		BLUE = BACKGROUND_INTENSITY | BACKGROUND_BLUE,
		GREEN = BACKGROUND_INTENSITY | BACKGROUND_GREEN,
		CYAN = BACKGROUND_INTENSITY | BACKGROUND_GREEN | BACKGROUND_BLUE,
		RED = BACKGROUND_INTENSITY | BACKGROUND_RED,
		MAGENTA = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_BLUE,
		YELLOW = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN,
		WHITE = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
	};
};

#define FULL_FUNC_INFO(_MSG) std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__) + ": " + _MSG
#define log_action_error(msg) logfunc::impllog_action_error( FULL_FUNC_INFO(msg) );

#define log_info(msg) logfunc::LogInfo( FULL_FUNC_INFO(msg) );

#define log_action_error_with_box(parentView, msg) _impllog_action_error_with_box(parentView, FULL_FUNC_INFO(msg));


#define log_action(_MSG)  logfunc::impl_log_action( \
	std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__) + ": " + _MSG);\

#define _log_function_call() logfunc::impl_log_action( \
	std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__));\


#define log_action_success(_MSG) logfunc::impl_log_action_success(_MSG);
#define log_action_warning(_MSG) logfunc::impl_log_action_warning(_MSG);
#define log_write(_MSG) logfunc::impl_log_write(_MSG);

namespace logfunc
{
	extern void LogInfo(const std::string& _strMsg);
	extern void impl_log_action(const std::string& _strMsg);
	extern void impl_log_action_success(const std::string& _strMsg);
	extern bool impllog_action_error(const std::string& _strMsg);
	extern bool impl_log_action_warning(const std::string& _strMsg);
	extern bool impl_log_action_warning(const std::wstring& _strMsg);
	extern void impl_log_write(const std::string& _strMsg);
}
