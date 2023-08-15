#include "Logging.h"
#include <fstream>
#include <fstream>
#include <iostream>
#include <sstream>
#include <iostream>

class WinConcole
{
public:
	static void Print(const std::wstring& str, WORD wColorFlags = FG_BLACK | BG_WHITE)
	{
		HANDLE h = GetStdHandle(STD_OUTPUT_HANDLE);		
		/*
		 * Set the new color information
		 */
		SetConsoleTextAttribute(h, wColorFlags);
		DWORD dwChars = 0;
		WriteConsole(h, str.data(), (DWORD)str.size(), &dwChars, NULL);
		
		/*
		* Set default color info
		*/
		SetConsoleTextAttribute(h, BG_BLACK | FG_WHITE);
		
	}

	static void PrintLn(const std::wstring& str, WORD color = BG_BLACK | FG_WHITE)
	{
		Print(str + L"\n", color);
	}
};

void ImplLog::LogActionInfo(const std::string& _strMsg)
{
	WinConcole::Print(L"FBX SDK ACTION:", BG_DARKBLUE | FG_WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "ACTION: " << (_strMsg).c_str();

	WriteToLogFile(logString.str());
}

void ImplLog::LogActionConcoleColor(const std::string& _strMsg, WORD wColorFlags)
{
	WinConcole::Print(L"FBX SDK ACTION:", wColorFlags);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "ACTION: " << (_strMsg).c_str();

	WriteToLogFile(logString.str());
}


void ImplLog::LogAction_success(const std::string& _strMsg)
{	
	WinConcole::Print(L"FBX SDK ACTION: SUCCESS:", BG_DARKGREEN | FG_WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	//WinConcole::Print(L"Success.", BG_BLUE | FG_WHITE);
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK ACTION: SUCCESS:" << _strMsg << ". Success.";
	
	WriteToLogFile(logString.str());	
}

bool ImplLog::LogActionErrorFalse(const std::string& _strMsg)
{	
	WinConcole::Print(L"FBX SDK ERROR:", BG_RED | FG_YELLOW);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK ERROR: " << _strMsg;

	WriteToLogFile(logString.str());

	return false;
}

bool ImplLog::LogAction_warning(const std::string& _strMsg)
{
	WinConcole::Print(L"FBX SDK WARNING:", BG_MAGENTA | FG_WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK WARNING:: " << _strMsg;

	 WriteToLogFile(logString.str());

	return false;
}

void ImplLog::LogWrite(const std::string& _strMsg)
{
	WriteToLogFile(_strMsg);
}

void ImplLog::WriteToLogFile(const std::string& logString)
{
    std::ofstream oOutFile(L"fbxsdk.log.txt", std::ios::app);
    oOutFile << logString;
    oOutFile.close();
}
